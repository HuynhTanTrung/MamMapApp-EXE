using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.SnackPlace
{
    public class AttributeGroupResponse
    {
        public List<AttributeItem> Tastes { get; set; }
        public List<AttributeItem> Diets { get; set; }
        public List<AttributeItem> FoodTypes { get; set; }
    }
    public class AttributeItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
