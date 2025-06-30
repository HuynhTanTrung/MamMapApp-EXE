using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class SnackPlaceClick
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SnackPlaceId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ClickedAt { get; set; }
    }

}
