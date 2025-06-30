using MamMap.Data.Entities;
using MamMap.ViewModels.System;
using MamMap.ViewModels.System.User;
using MamMapApp.Models;
using System.Security.Claims;

namespace MamMapApp.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponse?> Authenticate(LoginRequest request);
        Task<AspNetUsers?> GetUserByIdAsync(Guid id);
        Task<AspNetUsers?> CreateUserAsync(AspNetUsers user, string password);
        Task<AuthResponse?> RefreshTokenAsync(string accessToken, string refreshToken);
        Task<bool> GenerateAndSendOtpAsync(string email);
        Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(string userId, ChangePasswordRequest request);
        Task<bool> ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request);
        Task<object> GetAllUsersAsync();
        Task<object> SearchUsersAsync(SearchUserRequest request);
        Task<AspNetUsers?> UpdateUserAsync(UpdateUserDTO dto);
    }
}
