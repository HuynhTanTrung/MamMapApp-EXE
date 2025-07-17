using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class ChatSession
    {
        [Key]
        public Guid SessionId { get; set; }

        [Required]
        public string UserId { get; set; }

        public string? Title { get; set; }

        [Required]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
