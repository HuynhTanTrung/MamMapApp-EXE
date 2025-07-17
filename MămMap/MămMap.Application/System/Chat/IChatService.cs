using MamMap.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Chat
{
    public interface IChatService
    {
        Task<ChatSession> CreateNewSessionAsync(string userId, string? title = null);
        Task AddMessageToSessionAsync(Guid sessionId, string sender, string text);
        Task<List<ChatSession>> GetAllChatSessionsAsync(string userId);
        Task<ChatSession?> GetChatSessionByIdAsync(Guid sessionId);
        Task<bool> DeleteChatSessionAsync(Guid sessionId);
    }
}
