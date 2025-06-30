using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class SnackPlaces
    {
        public Guid SnackPlaceId { get; set; }
        public Guid UserId { get; set; }
        public string PlaceName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? Coordinates { get; set; } = string.Empty;
        public TimeOnly? OpeningHour { get; set; }
        public int? AveragePrice { get; set; }
        public string Image { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
        public Guid? BusinessModelId { get; set; }
        public string? MainDish { get; set; } = string.Empty;
        public ICollection<SnackPlaceAttributes> SnackPlaceAttributes { get; set; }
        public AspNetUsers User { get; set; }
        public BusinessModels? BusinessModels { get; set; }
    }
}
