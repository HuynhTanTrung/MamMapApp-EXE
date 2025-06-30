using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.SnackPlace
{
    public class FilterSnackPlaceRequest
    {
        public int? PriceFrom { get; set; }
        public int? PriceTo { get; set; }

        public List<Guid>? TasteIds { get; set; } = new();
        public List<Guid>? DietIds { get; set; } = new();
        public List<Guid>? FoodTypeIds { get; set; } = new();
    }

}
