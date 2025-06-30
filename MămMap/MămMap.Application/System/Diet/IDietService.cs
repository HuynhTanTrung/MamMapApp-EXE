using MamMap.Data.Entities;
using MamMap.ViewModels.System.Diet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Diet
{
    public interface IDietService
    {
        Task<(bool isSuccess, string? errorMessage, Diets? createdDiet)> CreateDietAsync(Diets diet);
        Task<object> GetAllDietsAsync();
        Task<Diets?> GetDietByIdAsync(Guid id);
        Task<(bool isSuccess, string? errorMessage)> UpdateDietAsync(Guid id, string newDescription);
        Task<(bool isSuccess, string? errorMessage)> DeleteDietAsync(Guid id);
        Task<object> SearchDietsAsync(SearchDietRequest request);
    }
}
