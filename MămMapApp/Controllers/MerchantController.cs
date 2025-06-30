using Azure.Core;
using MamMap.Application.System.Email;
using MamMap.Application.System.Merchant;
using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMapApp.Models;
using MamMapApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/merchants")]
    public class MerchantController : ControllerBase
    {
        private readonly MamMapDBContext _context;
        private readonly IMerchantService _merchantService;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<AspNetUsers> _userManager;

        public MerchantController(MamMapDBContext context, UserManager<AspNetUsers> userManager, IMerchantService merchantService, IEmailSender emailSender)
        {
            _merchantService = merchantService;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateMerchant([FromBody] CreateUserDTO dto)
        {
            var existingUser = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUser != null)
            {
                return BadRequest(new
                {
                    status = 400,
                    message = "Tên đăng nhập đã được sử dụng."
                });
            }

            var newUser = new AspNetUsers
            {
                Fullname = dto.FullName,
                Email = dto.Email,
                UserName = dto.UserName
            };

            var created = await _merchantService.CreateUserAsync(newUser, dto.Password);

            if (created != null)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                var confirmationLink = Url.Action(
                    nameof(ConfirmEmail),
                    "Merchant",
                    new { userId = newUser.Id, token },
                    Request.Scheme);

                await _emailSender.SendEmailAsync(newUser.Email, "Xác minh email MamMap", $@"
                    <p>Chào {newUser.Fullname},</p>
                    <p>Hãy bấm vào nút bên dưới để xác minh email của bạn:</p>
                    <a href='{confirmationLink}' style='padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none;'>Xác minh email</a>
                    ");

                return Ok(new
                {
                    status = 200,
                    message = "Tạo người bán hàng thành công. Đã gửi email xác minh tới địa chỉ email của bạn."
                });
            }

            return BadRequest(new
            {
                status = 400,
                message = "Tạo người bán hàng thất bại"
            });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new { status = 400, message = "Người dùng không tồn tại." });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return Redirect("https://fe-m-m.vercel.app/auth/active");
            }

            return BadRequest(new { status = 400, message = "Xác minh thất bại. Token không hợp lệ hoặc đã hết hạn." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] MamMap.ViewModels.System.User.LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Unauthorized(new
                {
                    status = 401,
                    message = "Tên đăng nhập hoặc mật khẩu không chính xác."
                });
            }

            if (!user.EmailConfirmed)
            {
                return BadRequest(new
                {
                    status = 400,
                    message = "Email của bạn chưa được xác minh. Vui lòng kiểm tra email để xác minh tài khoản."
                });
            }

            var result = await _merchantService.Authenticate(request);
            if (result == null)
            {
                return Unauthorized(new
                {
                    status = 401,
                    message = "Tên đăng nhập hoặc mật khẩu không chính xác."
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Đăng nhập thành công",
                data = result
            });
        }

        [HttpGet("checkCreatedSnackplace")]
        [Authorize]
        public async Task<IActionResult> CheckMySnackPlace()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new
                {
                    status = 401,
                    message = "Không tìm thấy thông tin người dùng."
                });
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest(new
                {
                    status = 400,
                    message = "UserId không hợp lệ."
                });
            }

            var snackPlace = await _context.SnackPlaces
                .Include(sp => sp.BusinessModels)
                .Include(sp => sp.SnackPlaceAttributes).ThenInclude(attr => attr.Taste)
                .Include(sp => sp.SnackPlaceAttributes).ThenInclude(attr => attr.Diet)
                .Include(sp => sp.SnackPlaceAttributes).ThenInclude(attr => attr.FoodType)
                .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.Status == true);

            if (snackPlace == null)
            {
                return Ok(new
                {
                    status = 200,
                    message = "Người dùng hiện chưa tạo quán ăn.",
                    data = (object?)null
                });
            }

            var result = new
            {
                snackPlaceId = snackPlace.SnackPlaceId,
                placeName = snackPlace.PlaceName,
                ownerName = snackPlace.OwnerName,
                address = snackPlace.Address,
                email = snackPlace.Email,
                phoneNumber = snackPlace.PhoneNumber,
                description = snackPlace.Description,
                coordinates = snackPlace.Coordinates,
                openingHour = snackPlace.OpeningHour,
                averagePrice = snackPlace.AveragePrice,
                image = snackPlace.Image,
                mainDish = snackPlace.MainDish,
                status = snackPlace.Status,
                userId = snackPlace.UserId,
                businessModelId = snackPlace.BusinessModelId,
                businessModelName = snackPlace.BusinessModels?.Name,
                attributes = new
                {
                    tastes = snackPlace.SnackPlaceAttributes
                        .Where(attr => attr.Taste != null)
                        .Select(attr => new
                        {
                            tasteId = attr.TasteId,
                            tasteName = attr.Taste!.Description
                        }).Distinct().ToList(),

                    diets = snackPlace.SnackPlaceAttributes
                        .Where(attr => attr.Diet != null)
                        .Select(attr => new
                        {
                            dietId = attr.DietId,
                            dietName = attr.Diet!.Description
                        }).Distinct().ToList(),

                    foodTypes = snackPlace.SnackPlaceAttributes
                        .Where(attr => attr.FoodType != null)
                        .Select(attr => new
                        {
                            foodTypeId = attr.FoodTypeId,
                            foodTypeName = attr.FoodType!.Description
                        }).Distinct().ToList()
                }
            };

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin quán ăn của người dùng thành công.",
                data = result
            });
        }
    }
}