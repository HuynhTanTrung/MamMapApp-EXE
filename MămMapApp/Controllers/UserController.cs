using MamMap.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using MamMapApp.Models;
using MamMapApp.Services.Interfaces;
using MamMap.ViewModels.System.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MamMap.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.Data;
using MamMap.Application.System.Email;
using Microsoft.EntityFrameworkCore;
using MamMap.Data.EF;

namespace UserCrudApp.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly MamMapDBContext _context;
        private readonly UserManager<AspNetUsers> _userManager;

        public UserController(UserManager<AspNetUsers> userManager, IUserService userService, IEmailSender emailSender, MamMapDBContext context)
        {
            _userService = userService;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO dto)
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

            var created = await _userService.CreateUserAsync(newUser, dto.Password);

            if (created != null)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                var confirmationLink = Url.Action(
                    nameof(ConfirmEmail),
                    "User",
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
                    message = "Tạo người dùng thành công. Đã gửi email xác minh tới địa chỉ email của bạn."
                });
            }

            return BadRequest(new
            {
                status = 400,
                message = "Tạo người dùng thất bại"
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

            var result = await _userService.Authenticate(request);
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

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _userService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);

            if (result == null)
            {
                return Unauthorized(new
                {
                    status = 401,
                    message = "Token không hợp lệ hoặc đã hết hạn."
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Làm mới token thành công",
                data = result
            });
        }

        [HttpGet("get-current-login")]
        [Authorize]
        public async Task<IActionResult> GetCurrentLogin()
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

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)   
            {
                return NotFound(new
                {
                    status = 404,
                    message = "Người dùng không tồn tại."
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userPackages = await _context.UserPremiumPackages
                .Where(x => x.UserId == userId && x.IsActive)
                .Include(x => x.PremiumPackage)
                .Select(x => new
                {
                    x.PremiumPackageId,
                    x.PurchaseDate,
                    PackageName = x.PremiumPackage.Name
                })
                .ToListAsync();

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin người dùng thành công.",
                data = new
                {
                    user.Id,
                    user.Fullname,
                    user.UserName,
                    user.DateOfBirth,
                    user.Email,
                    user.Address,
                    user.EmailConfirmed,
                    user.PhoneNumber,
                    user.Image,
                    user.Status,
                    Roles = roles,
                    UserPackages = userPackages
                }
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { status = 401, message = "Không xác định được người dùng." });
            }

            var (succeeded, errors) = await _userService.ChangePasswordAsync(userId, request);

            if (succeeded)
            {
                return Ok(new { status = 200, message = "Đổi mật khẩu thành công." });
            }

            return BadRequest(new
            {
                status = 400,
                message = "Đổi mật khẩu thất bại.",
                errors
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] MamMap.ViewModels.System.User.ForgotPasswordRequest request)
        {
            var success = await _userService.GenerateAndSendOtpAsync(request.Email);
            if (!success)
            {
                return BadRequest(new { status = 400, message = "Email không tồn tại hoặc chưa xác nhận." });
            }

            return Ok(new { status = 200, message = "Mã OTP đã được gửi đến email của bạn." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequest request)
        {
            var success = await _userService.ResetPasswordWithOtpAsync(request);

            if (!success)
                return BadRequest(new { status = 400, message = "OTP không hợp lệ hoặc đã hết hạn." });

            return Ok(new { status = 200, message = "Mật khẩu đã được đặt lại thành công." });
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                return NotFound(new
                {
                    status = 404,
                    message = "Người dùng không tồn tại."
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin người dùng thành công.",
                data = new
                {
                    user.Id,
                    user.Fullname,
                    user.UserName,
                    user.DateOfBirth,
                    user.Email,
                    user.Address,
                    user.EmailConfirmed,
                    user.PhoneNumber,
                    user.Image,
                    user.Status,
                    Roles = roles
                }
            });
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchUser([FromBody] SearchUserRequest request)
        {
            var result = await _userService.SearchUsersAsync(request);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser(UpdateUserDTO dto)
        {
            var updatedUser = await _userService.UpdateUserAsync(dto);
            if (updatedUser != null)
            {
                return Ok(new
                {
                    status = 200,
                    message = "Cập nhật người dùng thành công.",
                    data = new
                    {
                        updatedUser.Id,
                        updatedUser.Fullname,
                        updatedUser.Address,
                        updatedUser.PhoneNumber,
                        updatedUser.DateOfBirth,
                        updatedUser.Email
                    }
                });
            }

            return BadRequest(new
            {
                status = 400,
                message = "Không tìm thấy người dùng hoặc cập nhật thất bại."
            });
        }
    }
}
