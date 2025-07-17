using MamMap.Data.EF;
using MamMap.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Chat
{
    public class ChatService : IChatService
    {
        private readonly MamMapDBContext _context;

        public ChatService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<ChatSession> CreateNewSessionAsync(string userId, string? title = null)
        {
            var newSession = new ChatSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                StartTime = DateTime.UtcNow
            };
            _context.ChatSessions.Add(newSession);
            await _context.SaveChangesAsync();
            return newSession;
        }

        public async Task AddMessageToSessionAsync(Guid sessionId, string sender, string text)
        {
            var chatMessage = new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                SessionId = sessionId,
                Sender = sender,
                Text = text,
                Timestamp = DateTime.UtcNow
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatSession>> GetAllChatSessionsAsync(string userId)
        {
            return await _context.ChatSessions
                                 .Where(s => s.UserId == userId)
                                 .OrderByDescending(s => s.StartTime)
                                 .ToListAsync();
        }

        public async Task<ChatSession?> GetChatSessionByIdAsync(Guid sessionId)
        {
            return await _context.ChatSessions
                                 .Include(s => s.Messages.OrderBy(m => m.Timestamp)) 
                                 .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<bool> DeleteChatSessionAsync(Guid sessionId)
        {
            var session = await _context.ChatSessions
                                        .Include(s => s.Messages)
                                        .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                return false;
            }

            _context.ChatMessages.RemoveRange(session.Messages);
            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
