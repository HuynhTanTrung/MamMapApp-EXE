using MamMap.Data.Entities;
using MamMap.ViewModels.System.Dish;
using MamMap.ViewModels.System.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Dish
{
    public interface IDishService
    {
        Task<(bool isSuccess, string? errorMessage, Dishes? createdDish)> CreateDishAsync(Dishes dish);
        Task<object> GetAllDishesAsync();
        Task<Dishes?> GetDishByIdAsync(Guid id);
        Task<(bool isSuccess, string? errorMessage, Dishes? updatedDish)> UpdateDishAsync(UpdateDishDTO dto);
        Task<(bool isSuccess, string? errorMessage)> DeleteDishAsync(Guid id);
        Task<object> SearchDishesAsync(SearchDishRequest request);
        Task<IEnumerable<Dishes>> GetDishesBySnackPlaceIdAsync(Guid snackPlaceId);
    }
}
