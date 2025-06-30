using MamMap.Data.Entities;
using MamMap.ViewModels.System.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Package
{
    public interface IPackageService
    {
        Task<(bool isSuccess, string? errorMessage, PremiumPackage? package)> CreateAsync(CreatePremiumPackageDTO dto);
        Task<object> GetAllAsync();
        Task<PremiumPackage?> GetByIdAsync(int id);
        Task<(bool isSuccess, string? errorMessage)> UpdatePremiumPackageAsync(int packageId, UpdatePremiumPackageDTO dto);
        Task<object> SearchPackageAsync(SearchPackageRequest request);
        Task<bool> DeleteAsync(int id);
    }

}
