using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.AppRating
{
    public class CreateAppRatingDTO
    {
        public int Star { get; set; }
        public string? Description { get; set; }
        public string AppType { get; set; } = null!;
    }
}
