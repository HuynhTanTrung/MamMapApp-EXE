using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.SnackPlace
{
    public class AddDishesToSnackPlaceDTO
    {
        public Guid SnackPlaceId { get; set; }
        public List<Guid> DishIds { get; set; } = new();
    }
}
