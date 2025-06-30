using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly MamMapDBContext _context;

        public PaymentService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<bool> MarkPaymentAsPaidAsync(string paymentCode)
        {
            var payment = await _context.Payment
                .FirstOrDefaultAsync(p => p.PaymentCode == paymentCode);

            if (payment == null)
                return false;

            if (!payment.PaymentStatus)
            {
                payment.PaymentStatus = true;
                payment.PaidAt = DateTime.UtcNow;

                var existing = await _context.UserPremiumPackages
                    .FirstOrDefaultAsync(x => x.UserId == payment.UserId && x.PremiumPackageId == payment.PremiumPackageId);

                if (existing == null)
                {
                    var package = await _context.PremiumPackages.FindAsync(payment.PremiumPackageId);

                    _context.UserPremiumPackages.Add(new UserPremiumPackage
                    {
                        UserId = payment.UserId,
                        PremiumPackageId = payment.PremiumPackageId,
                        PurchaseDate = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<object?> CheckPaymentStatusAsync(Guid userId, Guid paymentId)
        {
            var payment = await _context.Payment
                .Where(p => p.UserId == userId && p.Id == paymentId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null) return null;

            return new
            {
                payment.PaymentStatus,
                payment.PaidAt,
                payment.PaymentCode,
                payment.TransactionId
            };
        }

        public async Task<List<object>> GetPaymentHistoryAsync(Guid userId)
        {
            var payments = await _context.Payment
                .Include(p => p.PremiumPackage)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Amount,
                    p.PaymentStatus,
                    p.CreatedAt,
                    p.PaidAt,
                    p.PaymentCode,
                    p.TransactionId,
                    PremiumPackageId = p.PremiumPackage.Id,
                    PremiumPackageName = p.PremiumPackage.Name
                })
                .ToListAsync();

            return payments.Cast<object>().ToList();
        }

        public async Task<List<UserPremiumPackage>> GetUserPremiumPackagesAsync(Guid userId)
        {
            return await _context.UserPremiumPackages
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync();
        }
    }
}