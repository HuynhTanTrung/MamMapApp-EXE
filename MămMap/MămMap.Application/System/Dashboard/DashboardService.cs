using MamMap.Data.EF;
using MamMap.ViewModels.System.Dashboard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly MamMapDBContext _context;

        public DashboardService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetTotalMerchantsAsync()
        {
            return await _context.SnackPlaces.Select(x => x.UserId).Distinct().CountAsync();
        }

        public async Task<int> GetTotalSnackPlacesAsync()
        {
            return await _context.SnackPlaces.CountAsync();
        }

        public async Task<decimal> GetTotalMoneyAsync()
        {
            return await _context.Payment
                .Where(p => p.PaymentStatus == true)
                .SumAsync(p => p.Amount);
        }

        public async Task<object> GetMerchantPercentageAsync()
        {
            var snackPlaceUserIds = await _context.SnackPlaces
                .Where(sp => sp.Status != null)
                .Select(sp => sp.UserId)
                .Distinct()
                .ToListAsync();

            var premiumUsersRaw = await _context.UserPremiumPackages
                .Select(upp => new { upp.UserId, upp.PremiumPackageId })
                .ToListAsync();

            var premiumUserIds = premiumUsersRaw
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

            var allMerchantUserIds = snackPlaceUserIds
                .Union(premiumUserIds)
                .Distinct()
                .ToList();

            var totalMerchants = allMerchantUserIds.Count;
            var totalPremiumMerchants = premiumUserIds.Count;
            var regularMerchantCount = totalMerchants - totalPremiumMerchants;

            var premiumGroups = await _context.UserPremiumPackages
                .Include(upp => upp.PremiumPackage)
                .GroupBy(upp => new { upp.PremiumPackageId, upp.PremiumPackage.Name })
                .Select(g => new
                {
                    PackageId = g.Key.PremiumPackageId,
                    PackageName = g.Key.Name,
                    PremiumMerchantCount = g.Select(x => x.UserId).Distinct().Count()
                })
                .ToListAsync();

            var premiumMerchantBreakdown = premiumGroups.Select(g => new
            {
                g.PackageId,
                g.PackageName,
                g.PremiumMerchantCount,
                Percent = totalMerchants > 0
                    ? Math.Round((double)g.PremiumMerchantCount / totalMerchants * 100, 2)
                    : 0
            });

            return new
            {
                TotalMerchants = totalMerchants,
                TotalPremiumMerchants = totalPremiumMerchants,
                RegularMerchants = regularMerchantCount,
                PremiumMerchantBreakdown = premiumMerchantBreakdown
            };
        }


        public async Task<IEnumerable<RevenueByDateDto>> GetRevenueByDateAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payment.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(p => p.CreatedAt <= toDate.Value);

            return await query
                .Where(p => p.PaymentStatus == true)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new RevenueByDateDto
                {
                    Date = g.Key,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<RevenueByMonthDto>> GetRevenueByMonthAsync(int year)
        {
            return await _context.Payment
                .Where(p => p.PaymentStatus == true && p.CreatedAt.Year == year)
                .GroupBy(p => p.CreatedAt.Month)
                .Select(g => new RevenueByMonthDto
                {
                    Month = g.Key,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToListAsync();
        }
    }

}
