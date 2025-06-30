using MamMap.Data.Entities;
using MamMap.ViewModels.System.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.BusinessModel
{
    public interface IBusinessModelService
    {
        Task<BusinessModels?> CreateBusinessModelAsync(BusinessModels model);
        Task<object> GetAllBusinessModelsAsync();
        Task<BusinessModels?> GetByIdAsync(Guid id);
        Task<(bool isSuccess, string? errorMessage)> UpdateBMAsync(Guid id, string newDescription);
        Task<bool> DeleteAsync(Guid id);
        Task<object> SearchBusinessModelsAsync(SearchBMRequest request);
    }
}
