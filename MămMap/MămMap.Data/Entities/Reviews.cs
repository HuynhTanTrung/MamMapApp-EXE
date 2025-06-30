using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class Reviews
    {
        public Guid Id { get; set; }
        public Guid SnackPlaceId { get; set; }
        public Guid UserId { get; set; }

        public int TasteRating { get; set; }
        public int PriceRating { get; set; }
        public int SanitaryRating { get; set; }
        public int TextureRating { get; set; }
        public int ConvenienceRating { get; set; }

        public string? Image { get; set; }
        public DateTime ReviewDate { get; set; }
        public string? Comment { get; set; }
        public int RecommendCount { get; set; }
        public bool IsRecommended { get; set; }
        public bool Status { get; set; }

        public SnackPlaces SnackPlace { get; set; } = null!;
        public AspNetUsers User { get; set; } = null!;
    }

}
