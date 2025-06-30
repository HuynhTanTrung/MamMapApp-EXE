using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class Replies
    {
        public Guid Id { get; set; }
        public Guid? ReviewId { get; set; }
        public Guid? ParentReplyId { get; set; }
        public Guid UserId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Status { get; set; } = true;

 
        public Reviews? Review { get; set; }
        public AspNetUsers User { get; set; }
        public Replies? ParentReply { get; set; }
        public ICollection<Replies>? Reply { get; set; }
    }


}
