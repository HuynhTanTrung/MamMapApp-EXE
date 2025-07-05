using MamMap.Application.System.Review;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDTO dto)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var (isSuccess, errorMessage, createdReview) = await _reviewService.CreateReviewAsync(dto, userRole);

            if (!isSuccess)
            {
                return BadRequest(new { status = 400, message = errorMessage });
            }

            return Ok(new
            {
                status = 200,
                message = "Đánh giá đã được tạo thành công.",
                data = new
                {
                    id = createdReview!.Id,
                    snackPlaceId = createdReview.SnackPlaceId,
                    userId = createdReview.UserId,
                    taste = createdReview.TasteRating,
                    price = createdReview.PriceRating,
                    sanitary = createdReview.SanitaryRating,
                    texture = createdReview.TextureRating,
                    convenience = createdReview.ConvenienceRating,
                    image = createdReview.Image,
                    comment = createdReview.Comment,
                    date = createdReview.ReviewDate,
                    recommendCount = createdReview.RecommendCount
                }
            });
        }

        [HttpGet("getAllReviewsAndReplies")]
        public async Task<IActionResult> GetAllReviews()
        {
            var merchantIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(merchantIdClaim))
            {
                return Unauthorized(new { status = 401, message = "Unauthorized: Merchant ID not found." });
            }

            var merchantId = Guid.Parse(merchantIdClaim);

            var reviews = await _reviewService.GetAllReviewsWithRepliesAsync(merchantId);
            return Ok(reviews);
        }

        [HttpGet("getAllReviewsAndRepliesBySnackPlaceId")]
        public async Task<IActionResult> GetAllReviewsBySnackPlace(Guid snackPlaceId)
        {
            Guid currentUserId = Guid.Empty;

            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            var reviews = await _reviewService.GetAllReviewsWithRepliesBySnackPlaceIdAsync(snackPlaceId, currentUserId);
            return Ok(reviews);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetReviewById(Guid id)
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid currentUserId))
            {
                return Unauthorized(new
                {
                    status = 401,
                    message = "Người dùng không xác thực."
                });
            }

            var result = await _reviewService.GetReviewByIdAsync(id, currentUserId);
            return Ok(result);
        }

        [HttpGet("getBySnackPlaceId")]
        public async Task<IActionResult> GetReviewsBySnackPlaceId(Guid snackPlaceId)
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid currentUserId))
            {
                return Unauthorized(new
                {
                    status = 401,
                    message = "Người dùng không xác thực."
                });
            }

            var result = await _reviewService.GetReviewsBySnackPlaceIdAsync(snackPlaceId, currentUserId);
            return Ok(result);
        }

        [HttpGet("getAllComments")]
        public async Task<IActionResult> GetAllComments()
        {
            var result = await _reviewService.GetAllCommentsAsync();

            return Ok(result);
        }

        [HttpPost("recommend")]
        public async Task<IActionResult> Recommend([FromQuery] Guid reviewId, [FromQuery] Guid userId)
        {
            var result = await _reviewService.ToggleRecommendAsync(reviewId, userId);
            return Ok(result);
        }

        [HttpGet("getAverageRate")]
        public async Task<IActionResult> GetAverageReviewRate(Guid snackPlaceId)
        {
            var result = await _reviewService.GetAverageReviewRateAsync(snackPlaceId);
            return Ok(result);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var deletedReview = await _reviewService.DeleteReviewAsync(id);

            if (deletedReview == null)
            {
                return NotFound(new
                {
                    status = 404,
                    message = "Đánh giá không tồn tại."
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Xóa (ẩn) đánh giá thành công."
            });
        }
    }
}
