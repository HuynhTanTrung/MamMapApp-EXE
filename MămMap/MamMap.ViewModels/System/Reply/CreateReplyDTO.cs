using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Reply
{
    public class CreateReplyDTO
    {
        public Guid? ReviewId { get; set; }
        public Guid? ParentReplyId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }
}
