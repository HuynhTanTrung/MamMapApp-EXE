using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Reply;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MamMap.Application.System.Reply
{
    public class ReplyService : IReplyService
    {
        private readonly MamMapDBContext _context;

        public ReplyService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string? errorMessage, Replies? createdReply)> CreateReplyAsync(CreateReplyDTO dto)
        {
            if ((dto.ReviewId == null && dto.ParentReplyId == null) ||
                (dto.ReviewId != null && dto.ParentReplyId != null))
            {
                return (false, "Chỉ được phép trả lời một đánh giá hoặc một phản hồi, không phải cả hai.", null);
            }

            var reply = new Replies
            {
                Id = Guid.NewGuid(),
                ReviewId = dto.ReviewId,
                ParentReplyId = dto.ParentReplyId,
                UserId = dto.UserId,
                Comment = dto.Content,
                CreatedAt = DateTime.UtcNow,
                Status = true
            };

            _context.Replies.Add(reply);
            await _context.SaveChangesAsync();

            return (true, null, reply);
        }

        public async Task<object> GetRepliesByReviewIdAsync(Guid reviewId)
        {
            var replies = await _context.Replies
                .Where(r => r.ReviewId == reviewId && r.Status == true)
                .Select(r => new
                {
                    id = r.Id,
                    reviewId = r.ReviewId,
                    parentReplyId = r.ParentReplyId,
                    userId = r.UserId,
                    content = r.Comment,
                    createdAt = r.CreatedAt
                }).ToListAsync();

            return new
            {
                status = 200,
                message = "Lấy danh sách phản hồi theo đánh giá thành công.",
                data = replies
            };
        }

        public async Task<object> GetRepliesByParentReplyIdAsync(Guid parentReplyId)
        {
            var replies = await _context.Replies
                .Where(r => r.ParentReplyId == parentReplyId && r.Status == true)
                .Select(r => new
                {
                    id = r.Id,
                    reviewId = r.ReviewId,
                    parentReplyId = r.ParentReplyId,
                    userId = r.UserId,
                    content = r.Comment,
                    createdAt = r.CreatedAt
                }).ToListAsync();

            return new
            {
                status = 200,
                message = "Lấy danh sách phản hồi theo phản hồi cha thành công.",
                data = replies
            };
        }

        public async Task<object> GetReplyByIdAsync(Guid id)
        {
            var r = await _context.Replies.FindAsync(id);
            if (r == null || r.Status != true)
            {
                return new { status = 404, message = "Không tìm thấy phản hồi." };
            }

            return new
            {
                status = 200,
                message = "Lấy phản hồi thành công.",
                data = new
                {
                    id = r.Id,
                    reviewId = r.ReviewId,
                    parentReplyId = r.ParentReplyId,
                    userId = r.UserId,
                    content = r.Comment,
                    createdAt = r.CreatedAt
                }
            };
        }

        public async Task<object> DeleteReplyAsync(Guid id)
        {
            var reply = await _context.Replies.FindAsync(id);
            if (reply == null || reply.Status == false)
            {
                return new { status = 404, message = "Không tìm thấy phản hồi." };
            }

            reply.Status = false;
            await _context.SaveChangesAsync();

            return new { status = 200, message = "Phản hồi đã được xóa thành công." };
        }
    }
}
