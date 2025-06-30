using MamMap.Data.Entities;
using MamMap.ViewModels.System.FoodType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.FoodType
{
    public interface IFoodTypeService
    {
        Task<(bool isSuccess, string? errorMessage, FoodTypes? createdFoodType)> CreateFoodTypeAsync(FoodTypes foodType);
        Task<object> GetAllFoodTypesAsync();
        Task<FoodTypes?> GetByIdAsync(Guid id);
        Task<(bool isSuccess, string? errorMessage)> UpdateTypeAsync(Guid id, string newDescription);
        Task<bool> DeleteAsync(Guid id);
        Task<object> SearchFoodTypesAsync(SearchFoodTypeRequest request);
    }
}
