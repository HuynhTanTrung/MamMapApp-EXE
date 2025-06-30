using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Dish
{
    public class UpdateDishDTO
    {
        public Guid DishId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public int? Price { get; set; }
        public Guid SnackPlaceId { get; set; }
    }
}
