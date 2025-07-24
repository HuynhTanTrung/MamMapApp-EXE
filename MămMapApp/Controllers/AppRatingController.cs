using MamMap.Application.System.AppRating;
using MamMap.ViewModels.System.AppRating;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppRatingController : ControllerBase
    {
        private readonly IAppRatingService _appReviewService;

        public AppRatingController(IAppRatingService appReviewService)
        {
            _appReviewService = appReviewService;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateAppReview([FromBody] CreateAppRatingDTO dto)
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { status = 401, message = "Người dùng chưa được xác thực." });
            }

            var (isSuccess, errorMessage, created) = await _appReviewService.CreateReviewAsync(userId, dto);

            if (!isSuccess)
            {
                return BadRequest(new { status = 400, message = errorMessage });
            }

            return Ok(new
            {
                status = 200,
                message = "Đánh giá ứng dụng đã được tạo thành công.",
                data = new
                {
                    id = created!.Id,
                    userId = created.UserId,
                    star = created.Star,
                    description = created.Description,
                    appType = created.AppType,
                    createdAt = created.CreatedAt
                }
            });
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var review = await _appReviewService.GetByIdAsync(id);
            if (review == null)
            {
                return NotFound(new { status = 404, message = "Không tìm thấy đánh giá." });
            }

            return Ok(new { status = 200, data = review });
        }

        [HttpGet("getByUserId")]
        public async Task<IActionResult> GetByUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { status = 401, message = "Người dùng không xác thực." });
            }

            var result = await _appReviewService.GetByUserIdAsync(userId);
            return Ok(new { status = 200, data = result });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAppRatings(
            [FromQuery] string? appType,
            [FromQuery] int? minStar,
            [FromQuery] int? maxStar,
            [FromQuery] int pageNum = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _appReviewService.SearchAsync(appType, minStar, maxStar, pageNum, pageSize);
            return Ok(result);
        }


        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _appReviewService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new { status = 404, message = "Không tìm thấy đánh giá để xóa." });
            }

            return Ok(new { status = 200, message = "Xóa đánh giá thành công." });
        }
    }
}
