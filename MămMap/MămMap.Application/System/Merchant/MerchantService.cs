using MamMap.Application.System.Email;
using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.User;
using MamMapApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MamMap.Application.System.Merchant
{
    public class MerchantService : IMerchantService
    {
        private readonly MamMapDBContext _context;
        private readonly UserManager<AspNetUsers> _merchantManager;
        private readonly SignInManager<AspNetUsers> _signInManager;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;
        private static readonly Dictionary<string, string> _otpStore = new();

        public MerchantService(MamMapDBContext context, UserManager<AspNetUsers> userManager, SignInManager<AspNetUsers> signInManager, IConfiguration config, IEmailSender emailSender)
        {
            _context = context;
            _merchantManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _emailSender = emailSender;
        }

        public async Task<AuthResponse?> Authenticate(LoginRequest request)
        {
            var user = await _merchantManager.FindByNameAsync(request.UserName);
            if (user == null) return null;

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded) return null;

            var isMerchant = await _merchantManager.IsInRoleAsync(user, "Merchant");
            if (!isMerchant) return null;

            var userRoles = await _merchantManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email ?? "")
    };
            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
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
            var result = await _merchantManager.CreateAsync(user, password);
            if (!result.Succeeded) return null;
            await _merchantManager.AddToRoleAsync(user, "Merchant");
            return user;
        }
    }
}
