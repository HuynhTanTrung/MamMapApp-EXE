using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class AppRatings
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int Star { get; set; }
        public string? Description { get; set; }
        public string AppType { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Status { get; set; }

        public AspNetUsers User { get; set; }
    }

}
