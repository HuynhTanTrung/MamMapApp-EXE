using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Review
{
    public class CreateReviewDTO
    {
        public Guid SnackPlaceId { get; set; }
        public Guid UserId { get; set; }

        public int TasteRating { get; set; }
        public int PriceRating { get; set; }
        public int SanitaryRating { get; set; }
        public int TextureRating { get; set; }
        public int ConvenienceRating { get; set; }

        public string? Image { get; set; }  
        public string? Comment { get; set; }
    }

}
