using MamMap.Data.Entities;
using MamMap.ViewModels.System.Taste;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Taste
{
    public interface ITasteService
    {
        Task<(bool isSuccess, string? errorMessage, Tastes? createdTaste)> CreateTasteAsync(Tastes taste);
        Task<object> GetAllTastesAsync();
        Task<Tastes?> GetByIdAsync(Guid id);
        Task<(bool isSuccess, string? errorMessage)> UpdateTasteAsync(Guid id, string newDescription);
        Task<bool> DeleteAsync(Guid id);
        Task<object> SearchTastesAsync(SearchTasteRequest request);
    }

}
