using MamMap.Data.Entities;
using MamMap.ViewModels.System.AppRating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.AppRating
{
    public interface IAppRatingService
    {
        Task<(bool isSuccess, string errorMessage, AppRatings? created)> CreateReviewAsync(Guid userId, CreateAppRatingDTO dto);
        Task<AppRatings?> GetByIdAsync(Guid id);
        Task<IEnumerable<AppRatings>> GetByUserIdAsync(Guid userId);
        Task<object> SearchAsync(string? appType, int? minStar, int? maxStar, int pageNum = 1, int pageSize = 10);
        Task<bool> DeleteAsync(Guid id);
    }
}
