using MamMap.Data.Entities;
using MamMap.ViewModels.System.Review;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Review
{
    public interface IReviewService
    {
        Task<(bool isSuccess, string errorMessage, Reviews? createdReview)> CreateReviewAsync(CreateReviewDTO dto, string userRole);
        Task<object> GetReviewByIdAsync(Guid id, Guid currentUserId);
        Task<object> GetReviewsBySnackPlaceIdAsync(Guid snackPlaceId, Guid currentUserId);
        Task<object> GetAllReviewsWithRepliesAsync(Guid merchantId);
        Task<object> GetAllCommentsAsync();
        Task<object> ToggleRecommendAsync(Guid reviewId, Guid userId);
        Task<object> GetAllReviewsWithRepliesBySnackPlaceIdAsync(Guid snackPlaceId, Guid currentUserId);
        Task<object> GetAverageReviewRateAsync(Guid snackPlaceId);
        Task<object> DeleteReviewAsync(Guid id);
    }
}
