using MamMap.Application.System.Payment;
using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly MamMapDBContext _context;
        private const string SePayApiKey = "HighMajorCommodoreoftheFirstLegionThirdMultiplicationDoubleAdmiralArtilleryVanguardCompany";

        public PaymentController(IPaymentService paymentService, MamMapDBContext context)
        {
            _paymentService = paymentService;
            _context = context;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Status = 401, Message = "Invalid or missing user ID in token" });
            }

            var premiumPackage = await _context.PremiumPackages.FindAsync(dto.PremiumPackageId);
            if (premiumPackage == null)
            {
                return NotFound(new { Status = 404, Message = "Premium package not found" });
            }

            var existingPayment = await _context.Payment
                .Where(p => p.UserId == userId &&
                            p.PremiumPackageId == dto.PremiumPackageId &&
                            !p.PaymentStatus)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                var existingQrUrl = $"https://qr.sepay.vn/img?acc=96247THIS1SME0KAY&bank=BIDV&amount={existingPayment.Amount}&des={existingPayment.PaymentCode}";

                return Ok(new
                {
                    Status = 200,
                    Message = "Unpaid payment already exists",
                    Data = new
                    {
                        existingPayment.Id,
                        existingPayment.UserId,
                        existingPayment.PremiumPackageId,
                        existingPayment.Amount,
                        existingPayment.PaymentCode,
                        existingPayment.PaymentStatus,
                        existingPayment.CreatedAt,
                        QrCodeUrl = existingQrUrl
                    }
                });
            }

            var paymentCode = $"DH{DateTime.Now:yyyyMMddHHmm}";

            var newPayment = new Payments
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PremiumPackageId = dto.PremiumPackageId,
                Amount = premiumPackage.Price,
                PaymentCode = paymentCode,
                PaymentStatus = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payment.Add(newPayment);
            await _context.SaveChangesAsync();

            var qrUrl = $"https://qr.sepay.vn/img?acc=96247THIS1SME0KAY&bank=BIDV&amount={newPayment.Amount}&des={newPayment.PaymentCode}";

            return Ok(new
            {
                Status = 200,
                Message = "Payment created",
                Data = new
                {
                    newPayment.Id,
                    newPayment.UserId,
                    newPayment.PremiumPackageId,
                    newPayment.Amount,
                    newPayment.PaymentCode,
                    newPayment.PaymentStatus,
                    newPayment.CreatedAt,
                    QrCodeUrl = qrUrl
                }
            });
        }

        [HttpPost("sepay-webhook")]
        public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookDTO request)
        {
            string? paymentCode = null;

            if (!string.IsNullOrEmpty(request.content))
            {
                paymentCode = request.content.Split(' ').LastOrDefault();
            }

            if (string.IsNullOrEmpty(paymentCode))
            {
                return BadRequest(new { Status = 400, Message = "Payment code not found in webhook content." });
            }

            var matched = await _paymentService.MarkPaymentAsPaidAsync(paymentCode);

            if (!matched)
            {
                return NotFound(new { Status = 404, Message = "Payment not found" });
            }

            return Ok(new { Status = 200, Message = "Payment marked as paid" });
        }


        [HttpGet("checkStatus")]
        public async Task<IActionResult> CheckPaymentStatus(Guid paymentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Status = 401, Message = "ID người dùng không hợp lệ hoặc bị thiếu" });
            }

            var result = await _paymentService.CheckPaymentStatusAsync(userId, paymentId);
            if (result == null)
                return NotFound(new { Status = 404, Message = "Không tìm thấy thanh toán" });

            return Ok(new { Status = 200, Message = "Lấy trạng thái thanh toán thành công", Data = result });
        }


        [HttpGet("paymentHistory")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Status = 401, Message = "ID người dùng không hợp lệ hoặc bị thiếu" });
            }

            var result = await _paymentService.GetPaymentHistoryAsync(userId);
            return Ok(new { Status = 200, Message = "Lấy lịch sử thanh toán thành công", Data = result });
        }

        [Authorize]
        [HttpGet("hasPackage")]
        public async Task<IActionResult> GetUserPremiumPackages()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Status = 401, Message = "ID người dùng không hợp lệ hoặc bị thiếu" });
            }

            var userPackages = await _paymentService.GetUserPremiumPackagesAsync(userId);

            return Ok(new
            {
                Status = 200,
                Message = "Danh sách gói Premium đã mua",
                Data = userPackages.Select(p => new
                {
                    p.UserId,
                    p.PremiumPackageId,
                    p.PurchaseDate,
                    p.IsActive,
                })
            });
        }
    }
}