using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class SnackPlaceAttributes
    {
        public Guid Id { get; set; }
        public Guid SnackPlaceId { get; set; }
        public SnackPlaces SnackPlace { get; set; }
        public Guid? TasteId { get; set; }
        public Tastes? Taste { get; set; }
        public Guid? DietId { get; set; }
        public Diets? Diet { get; set; }
        public Guid? FoodTypeId { get; set; }
        public FoodTypes? FoodType { get; set; }
    }

}
