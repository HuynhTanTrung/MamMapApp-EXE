using MamMap.Data.Entities;
using MamMap.Application.System.Gemini;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MamMap.Data.EF;
using MamMap.ViewModels.System.Gemini;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly MamMapDBContext _dbContext;

        public GeminiController(IGeminiService geminiService, MamMapDBContext dbContext)
        {
            _geminiService = geminiService;
            _dbContext = dbContext;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskBot([FromBody] AskBotRequest request)
        {
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
    }
}