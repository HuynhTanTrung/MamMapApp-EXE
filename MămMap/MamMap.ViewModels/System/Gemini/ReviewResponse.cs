using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Gemini
{
    public class ReviewResponse
    {
        public double AverageTasteRating { get; set; }
        public double AveragePriceRating { get; set; }
        public double AverageSanitaryRating { get; set; }
        public string? LatestComment { get; set; }
        public int TotalReviews { get; set; }
    }
}
