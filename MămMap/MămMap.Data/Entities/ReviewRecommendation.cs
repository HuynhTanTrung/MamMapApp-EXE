using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class ReviewRecommendation
    {
        public Guid ReviewRecommendationId { get; set; } = Guid.NewGuid();
        public Guid ReviewId { get; set; }
        public Guid UserId { get; set; }

        public Reviews Review { get; set; }
        public AspNetUsers User { get; set; }
    }

}
