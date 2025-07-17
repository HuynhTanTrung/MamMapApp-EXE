using MamMap.Data.Entities;
using MamMap.Application.System.Gemini;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MamMap.Data.EF; // Assuming MamMapDBContext is in this namespace
using MamMap.ViewModels.System.Gemini;
using MamMap.Application.System.Chat;
using System;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly IChatService _chatService;
        private readonly MamMapDBContext _dbContext;

        public GeminiController(IGeminiService geminiService, IChatService chatService, MamMapDBContext dbContext)
        {
            _geminiService = geminiService;
            _chatService = chatService;
            _dbContext = dbContext;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskBot([FromBody] AskBotRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found. Please authenticate or provide a valid user identifier.");
                }
            }

            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            var snackPlaces = await _dbContext.SnackPlaces
                .Where(x => x.Status)
                .ToListAsync();

            var reviews = await _dbContext.Reviews
                .Where(r => r.Status)
                .ToListAsync();

            var dishes = await _dbContext.Dishes
                .Where(d => d.Status)
                .ToListAsync();

            var (isSuccess, message, response) = await _geminiService.GetBotResponseAsync(
                request.Prompt,
                username,
                userId,
                request.SessionId,
                snackPlaces,
                reviews,
                dishes);

            return Ok(new
            {
                status = 200,
                message = isSuccess ? "Trả lời thành công." : message,
                data = isSuccess ? new { botReply = response } : null
            });
        }

        [HttpPost("createSession")]
        public async Task<IActionResult> CreateNewChatSession([FromQuery] string? title = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found. Cannot create session.");
                }
            }

            var newSession = await _chatService.CreateNewSessionAsync(userId, title);
            return Ok(new
            {
                status = 200,
                message = "Tạo phiên trò chuyện mới thành công.",
                data = new { sessionId = newSession.SessionId, title = newSession.Title, startTime = newSession.StartTime }
            });
        }

        [HttpGet("getAllSessions")]
        public async Task<IActionResult> GetAllChatSessions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found. Cannot retrieve sessions.");
                }
            }

            var sessions = await _chatService.GetAllChatSessionsAsync(userId);
            return Ok(new
            {
                status = 200,
                message = "Lấy danh sách phiên trò chuyện thành công.",
                data = sessions
            });
        }

        [HttpGet("getSessionById")]
        public async Task<IActionResult> GetChatSession(Guid sessionId)
        {
            var session = await _chatService.GetChatSessionByIdAsync(sessionId);
            if (session == null)
            {
                return NotFound($"Phiên trò chuyện với ID {sessionId} không tìm thấy.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || session.UserId != userId)
            {
                return Forbid("Bạn không có quyền truy cập phiên trò chuyện này.");
            }

            return Ok(new
            {
                status = 200,
                message = "Lấy chi tiết phiên trò chuyện thành công.",
                data = session
            });
        }

        [HttpDelete("deleteSession")]
        public async Task<IActionResult> DeleteChatSession(Guid sessionId)
        {
            var session = await _chatService.GetChatSessionByIdAsync(sessionId);
            if (session == null)
            {
                return NotFound($"Phiên trò chuyện với ID {sessionId} không tìm thấy.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || session.UserId != userId)
            {
                return Forbid("Bạn không có quyền xóa phiên trò chuyện này.");
            }

            var deleted = await _chatService.DeleteChatSessionAsync(sessionId);
            if (!deleted)
            {
                return StatusCode(500, "Xóa phiên trò chuyện thất bại.");
            }
            return Ok(new
            {
                status = 200,
                message = "Xóa phiên trò chuyện thành công.",
                data = new { sessionId = sessionId, deleted = true }
            });
        }
    }
}
