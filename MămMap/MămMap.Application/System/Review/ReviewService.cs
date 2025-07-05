using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Review;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MamMap.Application.System.Review
{
    public class ReviewService : IReviewService
    {
        private readonly MamMapDBContext _context;

        public ReviewService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string errorMessage, Reviews? createdReview)> CreateReviewAsync(CreateReviewDTO dto, string userRole)
        {
            if (userRole != "User")
            {
                return (false, "Chỉ người dùng mới được phép tạo đánh giá.", null);
            }

            if (!IsRatingValid(dto.TasteRating) ||
                !IsRatingValid(dto.PriceRating) ||
                !IsRatingValid(dto.SanitaryRating) ||
                !IsRatingValid(dto.TextureRating) ||
                !IsRatingValid(dto.ConvenienceRating))
            {
                return (false, "Đánh giá phải nằm trong khoảng từ 1 đến 5.", null);
            }

            var review = new Reviews
            {
                Id = Guid.NewGuid(),
                SnackPlaceId = dto.SnackPlaceId,
                UserId = dto.UserId,
                TasteRating = dto.TasteRating,
                PriceRating = dto.PriceRating,
                SanitaryRating = dto.SanitaryRating,
                TextureRating = dto.TextureRating,
                ConvenienceRating = dto.ConvenienceRating,
                Image = dto.Image,
                Comment = dto.Comment,
                ReviewDate = DateTime.UtcNow,
                RecommendCount = 0,
                IsRecommended = false,
                Status = true
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return (true, "Đánh giá đã được tạo thành công.", review);
        }

        private bool IsRatingValid(int rating)
        {
            return rating >= 1 && rating <= 5;
        }

        public async Task<object> GetReviewByIdAsync(Guid id, Guid currentUserId)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.Status == true);

            if (review == null)
            {
                return new
                {
                    status = 404,
                    message = "Không tìm thấy đánh giá."
                };
            }

            bool isRecommend = await _context.ReviewRecommendations
                .AnyAsync(rr => rr.ReviewId == review.Id && rr.UserId == currentUserId);

            return new
            {
                status = 200,
                message = "Lấy đánh giá thành công.",
                data = new
                {
                    id = review.Id,
                    snackPlaceId = review.SnackPlaceId,
                    userId = review.UserId,
                    userName = review.User.UserName,
                    taste = review.TasteRating,
                    price = review.PriceRating,
                    sanitary = review.SanitaryRating,
                    texture = review.TextureRating,
                    convenience = review.ConvenienceRating,
                    image = review.Image,
                    comment = review.Comment,
                    date = review.ReviewDate,
                    recommendCount = review.RecommendCount,
                    isRecommend,
                    status = review.Status
                }
            };
        }


        public async Task<object> GetReviewsBySnackPlaceIdAsync(Guid snackPlaceId, Guid currentUserId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.SnackPlaceId == snackPlaceId && r.Status == true)
                .Select(review => new
                {
                    id = review.Id,
                    snackPlaceId = review.SnackPlaceId,
                    userId = review.UserId,
                    userName = review.User.UserName,
                    taste = review.TasteRating,
                    price = review.PriceRating,
                    sanitary = review.SanitaryRating,
                    texture = review.TextureRating,
                    convenience = review.ConvenienceRating,
                    image = review.Image,
                    comment = review.Comment,
                    date = review.ReviewDate,
                    recommendCount = review.RecommendCount,

                    isRecommend = _context.ReviewRecommendations
                        .Any(rr => rr.ReviewId == review.Id && rr.UserId == currentUserId),

                    status = review.Status
                })
                .ToListAsync();

            return new
            {
                status = 200,
                message = "Lấy đánh giá theo quán thành công.",
                data = reviews
            };
        }

        public async Task<object> GetAllReviewsWithRepliesAsync(Guid merchantId)
        {
            var snackPlaceIds = await _context.SnackPlaces
                .Where(sp => sp.UserId == merchantId && sp.Status != false)
                .Select(sp => sp.SnackPlaceId)
                .ToListAsync();
            var flatReplies = await _context.Replies
                .Where(r => r.Status != false)
                .Select(r => new
                {
                    replyId = r.Id,
                    reviewId = r.ReviewId,
                    parentReplyId = r.ParentReplyId,
                    userId = r.UserId,
                    userName = r.User.UserName,
                    image = r.User.Image,
                    comment = r.Comment,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            List<object> GetChildReplies(Guid parentId)
            {
                return flatReplies
                    .Where(r => r.parentReplyId == parentId)
                    .Select(r => new
                    {
                        r.replyId,
                        r.reviewId,
                        r.parentReplyId,
                        r.userId,
                        r.userName,
                        r.image,
                        r.comment,
                        r.createdAt,
                        replies = GetChildReplies(r.replyId)
                    })
                    .ToList<object>();
            }

            var reviews = await _context.Reviews
                .Where(r => snackPlaceIds.Contains(r.SnackPlaceId))
                .Where(r => r.Status != false)
                .Select(r => new
                {
                    reviewId = r.Id,
                    snackPlaceId = r.SnackPlaceId,
                    userId = r.UserId,
                    userName = r.User.UserName,
                    taste = r.TasteRating,
                    price = r.PriceRating,
                    sanitary = r.SanitaryRating,
                    texture = r.TextureRating,
                    convenience = r.ConvenienceRating,
                    image = r.Image,
                    comment = r.Comment,
                    date = r.ReviewDate,
                    recommendCount = r.RecommendCount,
                    isRecommend = r.IsRecommended,
                    status = r.Status
                })
                .ToListAsync();

            var result = reviews.Select(review => new
            {
                review.reviewId,
                review.snackPlaceId,
                review.userId,
                review.userName,
                review.taste,
                review.price,
                review.sanitary,
                review.texture,
                review.convenience,
                review.image,
                review.comment,
                review.date,
                review.recommendCount,
                review.isRecommend,
                review.status,
                replies = flatReplies
                    .Where(r => r.reviewId == review.reviewId && r.parentReplyId == null)
                    .Select(r => new
                    {
                        r.replyId,
                        r.reviewId,
                        r.parentReplyId,
                        r.userId,
                        r.userName,
                        r.image,
                        r.comment,
                        r.createdAt,
                        replies = GetChildReplies(r.replyId)
                    })
                    .ToList<object>()
            });

            return new
            {
                status = 200,
                message = "Lấy danh sách đánh giá và phản hồi thành công.",
                data = result
            };
        }

        public async Task<object> GetAllReviewsWithRepliesBySnackPlaceIdAsync(Guid snackPlaceId, Guid currentUserId)
        {
            var flatReplies = await _context.Replies
                .Where(r => r.Status != false)
                .Select(r => new
                {
                    replyId = r.Id,
                    reviewId = r.ReviewId,
                    parentReplyId = r.ParentReplyId,
                    userId = r.UserId,
                    userName = r.User.UserName,
                    image = r.User.Image,
                    comment = r.Comment,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            List<object> GetChildReplies(Guid parentId)
            {
                return flatReplies
                    .Where(r => r.parentReplyId == parentId)
                    .Select(r => new
                    {
                        r.replyId,
                        r.reviewId,
                        r.parentReplyId,
                        r.userId,
                        r.userName,
                        r.image,
                        r.comment,
                        r.createdAt,
                        replies = GetChildReplies(r.replyId)
                    })
                    .ToList<object>();
            }

            var reviews = await _context.Reviews
                .Where(r => r.SnackPlaceId == snackPlaceId && r.Status != false)
                .Select(r => new
                {
                    reviewId = r.Id,
                    snackPlaceId = r.SnackPlaceId,
                    userId = r.UserId,
                    userName = r.User.UserName,
                    taste = r.TasteRating,
                    price = r.PriceRating,
                    sanitary = r.SanitaryRating,
                    texture = r.TextureRating,
                    convenience = r.ConvenienceRating,
                    image = r.Image,
                    comment = r.Comment,
                    date = r.ReviewDate,
                    recommendCount = r.RecommendCount,
                    isRecommended = currentUserId != Guid.Empty
                        ? _context.ReviewRecommendations.Any(rr => rr.ReviewId == r.Id && rr.UserId == currentUserId)
                        : false,
                    status = r.Status
                })
                .ToListAsync();

            var result = reviews.Select(review => new
            {
                review.reviewId,
                review.snackPlaceId,
                review.userId,
                review.userName,
                review.taste,
                review.price,
                review.sanitary,
                review.texture,
                review.convenience,
                review.image,
                review.comment,
                review.date,
                review.recommendCount,
                isRecommend = review.isRecommended,
                review.status,
                replies = flatReplies
                    .Where(r => r.reviewId == review.reviewId && r.parentReplyId == null)
                    .Select(r => new
                    {
                        r.replyId,
                        r.reviewId,
                        r.parentReplyId,
                        r.userId,
                        r.userName,
                        r.image,
                        r.comment,
                        r.createdAt,
                        replies = GetChildReplies(r.replyId)
                    })
                    .ToList<object>()
            });

            return new
            {
                status = 200,
                message = "Lấy danh sách đánh giá và phản hồi thành công.",
                data = result
            };
        }

        public async Task<object> GetAllCommentsAsync()
        {
            var comments = await _context.Reviews
                .Where(r => r.Status == true && !string.IsNullOrEmpty(r.Comment))
                .Select(r => new
                {
                    reviewId = r.Id,
                    snackPlaceId = r.SnackPlaceId,
                    comment = r.Comment
                })
                .ToListAsync();

            return new
            {
                status = 200,
                message = "Lấy tất cả bình luận thành công.",
                data = comments
            };
        }

        public async Task<object> ToggleRecommendAsync(Guid reviewId, Guid userId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null || review.Status == false)
            {
                return new
                {
                    status = 404,
                    message = "Không tìm thấy đánh giá."
                };
            }

            var existing = await _context.ReviewRecommendations
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

            bool isRecommended;

            if (existing != null)
            {
                _context.ReviewRecommendations.Remove(existing);
                review.RecommendCount = Math.Max(0, review.RecommendCount - 1);
                isRecommended = false;
            }
            else
            {
                _context.ReviewRecommendations.Add(new ReviewRecommendation
                {
                    ReviewId = reviewId,
                    UserId = userId
                });
                review.RecommendCount += 1;
                isRecommended = true;
            }

            await _context.SaveChangesAsync();

            return new
            {
                status = 200,
                message = isRecommended ? "Đã đề xuất đánh giá." : "Đã gỡ đề xuất đánh giá.",
                data = new
                {
                    reviewId,
                    isRecommended,
                    recommendCount = review.RecommendCount
                }
            };
        }

        public async Task<object> GetAverageReviewRateAsync(Guid snackPlaceId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.SnackPlaceId == snackPlaceId && r.Status)
                .ToListAsync();

            if (reviews.Count == 0)
            {
                return new { status = 404, message = "Không tìm thấy đánh giá nào." };
            }

            var totalRatings = reviews
                .Select(r =>
                {
                    var ratings = new[] {
                Math.Clamp(r.TasteRating, 1, 5),
                Math.Clamp(r.PriceRating, 1, 5),
                Math.Clamp(r.SanitaryRating, 1, 5),
                Math.Clamp(r.TextureRating, 1, 5),
                Math.Clamp(r.ConvenienceRating, 1, 5)
                    };
                    return ratings.Average();
                })
                .ToList();

            var averageRating = Math.Round(totalRatings.Average(), 2);

            var starBuckets = totalRatings
                .Select(r => (int)Math.Round(r, MidpointRounding.AwayFromZero))
                .Select(r => Math.Clamp(r, 1, 5))
                .ToList();

            var ratingGroups = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
            foreach (var stars in starBuckets) ratingGroups[stars]++;

            int totalReviews = reviews.Count;

            var ratingPercent = ratingGroups.ToDictionary(
                x => x.Key,
                x => Math.Round(x.Value * 100.0 / totalReviews, 2)
            );

            double recommendPercent = Math.Round(
                (ratingGroups[5] * 1.0 +
                 (ratingGroups[1] + ratingGroups[2] + ratingGroups[3] + ratingGroups[4]) * 0.125)
                * 100.0 / totalReviews,
                2
            );

            return new
            {
                status = 200,
                message = "Lấy thống kê đánh giá thành công.",
                data = new
                {
                    averageRating,
                    totalRatingsCount = totalReviews,
                    recommendPercent,
                    ratingDistributionPercent = ratingPercent
                }
            };
        }

        public async Task<object> DeleteReviewAsync(Guid id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null || review.Status == false)
            {
                return null;
            }

            review.Status = false;
            await _context.SaveChangesAsync();

            return new { status = 200, message = "Đánh giá đã được xóa thành công." };
        }
    }
}
