using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class FoodTypes
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; } = true;
        public ICollection<SnackPlaceAttributes> SnackPlaceAttributes { get; set; }
    }

}
