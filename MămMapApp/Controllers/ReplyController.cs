using MamMap.Application.System.Reply;
using MamMap.ViewModels.System.Reply;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MamMap.BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReplyController : ControllerBase
    {
        private readonly IReplyService _replyService;

        public ReplyController(IReplyService replyService)
        {
            _replyService = replyService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateReply([FromBody] CreateReplyDTO dto)
        {
            try
            {
                var (isSuccess, errorMessage, createdReply) = await _replyService.CreateReplyAsync(dto);

                if (!isSuccess)
                {
                    return BadRequest(new { status = 400, message = errorMessage });
                }

                return Ok(new
                {
                    status = 200,
                    message = "Phản hồi đã được tạo thành công.",
                    data = new
                    {
                        id = createdReply!.Id,
                        reviewId = createdReply.ReviewId,
                        parentReplyId = createdReply.ParentReplyId,
                        userId = createdReply.UserId,
                        content = createdReply.Comment,
                        createdAt = createdReply.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                string detailedMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new
                {
                    status = 500,
                    message = "Lỗi máy chủ: " + detailedMessage
                });
            }
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetReplyById(Guid id)
        {
            var result = await _replyService.GetReplyByIdAsync(id);
            var status = (int)result.GetType().GetProperty("status")?.GetValue(result, null)!;

            return status == 404 ? NotFound(result) : Ok(result);
        }

        [HttpGet("getByReviewId")]
        public async Task<IActionResult> GetRepliesByReviewId(Guid reviewId)
        {
            var result = await _replyService.GetRepliesByReviewIdAsync(reviewId);
            return Ok(result);
        }

        [HttpGet("getByParentReplyId")]
        public async Task<IActionResult> GetRepliesByParentReplyId(Guid parentReplyId)
        {
            var result = await _replyService.GetRepliesByParentReplyIdAsync(parentReplyId);
            return Ok(result);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteReply(Guid id)
        {
            var result = await _replyService.DeleteReplyAsync(id);
            var status = (int)result.GetType().GetProperty("status")?.GetValue(result, null)!;

            return status == 404 ? NotFound(result) : Ok(result);
        }
    }
}
