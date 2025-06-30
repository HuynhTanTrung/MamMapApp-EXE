using MamMap.Application.System.Email;
using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System;
using MamMap.ViewModels.System.User;
using MamMapApp.Models;
using MamMapApp.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MamMapApp.Services
{
    public class UserService : IUserService
    {
        private readonly MamMapDBContext _context;
        private readonly UserManager<AspNetUsers> _userManager;
        private readonly SignInManager<AspNetUsers> _signInManager;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;
        private static readonly Dictionary<string, string> _otpStore = new();

        public UserService(MamMapDBContext context, UserManager<AspNetUsers> userManager, SignInManager<AspNetUsers> signInManager, IConfiguration config, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _emailSender = emailSender;
        }

        public async Task<AuthResponse?> Authenticate(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null) return null;

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded) return null;

            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Any(role => role == "User" || role == "Admin"))
            {
                return null;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
            };
            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddHours(2);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = Guid.NewGuid().ToString();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };
        }

        public async Task<AspNetUsers?> CreateUserAsync(AspNetUsers user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) return null;
            await _userManager.AddToRoleAsync(user, "User");
            return user;
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;

            try
            {
                var principal = tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])),
                    ValidateLifetime = false
                }, out validatedToken);

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId!);

                if (user == null)
                    return null;

                var newClaims = new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddMinutes(30);

                var newToken = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Issuer"],
                    claims: newClaims,
                    expires: expires,
                    signingCredentials: creds
                );

                var newAccessToken = tokenHandler.WriteToken(newToken);
                var newRefreshToken = Guid.NewGuid().ToString();

                return new AuthResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> GenerateAndSendOtpAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                return false;

            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[email] = otp + "|" + DateTime.UtcNow.AddMinutes(5).ToString("o");

            await _emailSender.SendEmailAsync(email, "Mã OTP khôi phục mật khẩu",
                $"<p>Mã OTP của bạn là: <strong>{otp}</strong></p><p>Mã này có hiệu lực trong 5 phút.</p>");

            return true;
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, new[] { "Không tìm thấy người dùng." });
            }

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                var translatedErrors = result.Errors.Select(e =>
                {
                    return e.Code switch
                    {
                        "PasswordMismatch" => "Mật khẩu hiện tại không đúng.",
                        "PasswordTooShort" => "Mật khẩu phải có ít nhất 6 ký tự.",
                        "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải chứa ít nhất một ký tự đặc biệt.",
                        "PasswordRequiresDigit" => "Mật khẩu phải chứa ít nhất một chữ số.",
                        "PasswordRequiresUpper" => "Mật khẩu phải chứa ít nhất một chữ in hoa.",
                        "PasswordRequiresLower" => "Mật khẩu phải chứa ít nhất một chữ thường.",
                        _ => e.Description
                    };
                });

                return (false, translatedErrors);
            }

            return (true, Array.Empty<string>());
        }

        public async Task<bool> ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request)
        {
            if (!_otpStore.TryGetValue(request.Email, out var storedOtp))
                return false;

            var parts = storedOtp.Split('|');
            if (parts.Length != 2 || parts[0] != request.Otp)
                return false;

            var expiryTime = DateTime.Parse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind);

            if (DateTime.UtcNow > expiryTime)
                return false;

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                return false;

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (!result.Succeeded) return false;

            _otpStore.Remove(request.Email);
            return true;
        }

        public async Task<AspNetUsers> GetUserByIdAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            return user;
        }

        public async Task<object> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            var userData = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userData.Add(new
                {
                    id = user.Id,
                    userName = user.UserName,
                    mail = user.Email,
                    address = user.Address,
                    phoneNumber = user.PhoneNumber,
                    fullName = user.Fullname,
                    dob = user.DateOfBirth,
                    status = user.Status,
                    image = user.Image,
                    roles = roles
                });
            }

            return new
            {
                status = 200,
                message = "Lấy tất cả người dùng thành công.",
                data = userData
            };
        }


        public async Task<object> SearchUsersAsync(SearchUserRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchKeyword))
            {
                query = query.Where(u =>
                    u.UserName!.Contains(request.SearchKeyword) ||
                    u.Email == request.SearchKeyword ||
                    u.Id.ToString() == request.SearchKeyword
                );
            }

            if (request.Status.HasValue)
            {
                query = query.Where(u => u.Status == request.Status.Value);
            }

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminIds = admins.Select(u => u.Id).ToHashSet();

            if (!string.IsNullOrWhiteSpace(request.Role) && !request.Role.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(request.Role);
                var userIdsInRole = usersInRole.Select(u => u.Id).Where(id => !adminIds.Contains(id));
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
            else
            {
                query = query.Where(u => !adminIds.Contains(u.Id));
            }

            var total = await query.CountAsync();
            var users = await query
                .Skip((request.PageNum - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var pageData = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                pageData.Add(new
                {
                    id = user.Id,
                    userName = user.UserName,
                    mail = user.Email,
                    address = user.Address,
                    phoneNumber = user.PhoneNumber,
                    fullName = user.Fullname,
                    dob = user.DateOfBirth,
                    status = user.Status,
                    image = user.Image,
                    roles = roles
                });
            }

            return new
            {
                status = 200,
                message = "Lấy danh sách người dùng thành công.",
                data = new
                {
                    pageData,
                    pageInfo = new
                    {
                        pageNum = request.PageNum,
                        pageSize = request.PageSize,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / request.PageSize)
                    }
                }
            };
        }

        public async Task<AspNetUsers?> UpdateUserAsync(UpdateUserDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.Id.ToString());
            if (user == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Fullname))
                user.Fullname = dto.Fullname;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                user.Address = dto.Address;

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                user.PhoneNumber = dto.PhoneNumber;

            if (dto.DateOfBirth != null)
                user.DateOfBirth = dto.DateOfBirth;

            if (!string.IsNullOrWhiteSpace(dto.Image))
                user.Image = dto.Image;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? user : null;
        }
    }
}
