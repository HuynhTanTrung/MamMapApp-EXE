using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.SnackPlace
{
    public class CreateSnackPlaceDTO
    {
        public Guid UserId { get; set; }
        public string PlaceName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Coordinates { get; set; }
        public TimeOnly? OpeningHour { get; set; }
        public int? AveragePrice { get; set; }
        public string? Image { get; set; } = string.Empty;
        public string? MainDish { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Guid? BusinessModelId { get; set; }
        public List<Guid>? TasteIds { get; set; }
        public List<Guid>? DietIds { get; set; }
        public List<Guid>? FoodTypeIds { get; set; }
    }
}
