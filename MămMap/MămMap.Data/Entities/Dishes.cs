using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class Dishes
    {
        public Guid DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? Image { get; set; } = string.Empty;
        public int Price { get; set; }
        public bool Drink { get; set; } = false;
        public bool Status { get; set; } = true;
        public Guid SnackPlaceId { get; set; }
        public SnackPlaces SnackPlace { get; set; } = null!;
    }
}
