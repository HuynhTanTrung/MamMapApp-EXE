using MamMap.Data.Entities;
using MamMap.ViewModels.System.Reply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Reply
{
    public interface IReplyService
    {
        Task<(bool isSuccess, string? errorMessage, Replies? createdReply)> CreateReplyAsync(CreateReplyDTO dto);
        Task<object> GetRepliesByReviewIdAsync(Guid reviewId);
        Task<object> GetRepliesByParentReplyIdAsync(Guid parentReplyId);
        Task<object> GetReplyByIdAsync(Guid id);
        Task<object> DeleteReplyAsync(Guid id);
    }
}
