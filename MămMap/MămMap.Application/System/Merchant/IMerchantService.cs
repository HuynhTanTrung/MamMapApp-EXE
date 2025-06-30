using MamMap.Data.Entities;
using MamMap.ViewModels.System.User;
using MamMapApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Merchant
{
    public interface IMerchantService
    {
        Task<AuthResponse?> Authenticate(LoginRequest request);
        Task<AspNetUsers?> CreateUserAsync(AspNetUsers user, string password);
    }
}
