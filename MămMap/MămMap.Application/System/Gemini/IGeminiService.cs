using MamMap.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MamMap.Application.System.Gemini
{
    public interface IGeminiService
    {
        Task<(bool isSuccess, string message, string response)> GetBotResponseAsync(
            string prompt,
            string? userName,
            string userId,
            Guid? sessionId,
            List<SnackPlaces> snackPlaces,
            List<Reviews> reviews,
            List<Dishes> allDishes);
    }
}
