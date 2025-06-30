using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.SnackPlace
{
    public class UpdateSnackPlaceDTO
    {
        public Guid SnackPlaceId { get; set; }
        public string? PlaceName { get; set; }
        public string? OwnerName { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public TimeOnly? OpeningHour { get; set; }
        public string? Description { get; set; }
        public string? Coordinates { get; set; }
        public int? AveragePrice { get; set; }
        public string? Image { get; set; }
        public Guid? BusinessModelId { get; set; }
        public List<Guid>? TasteIds { get; set; }
        public List<Guid>? DietIds { get; set; }
        public List<Guid>? FoodTypeIds { get; set; }
        public string? MainDish { get; set; }

    }
}
