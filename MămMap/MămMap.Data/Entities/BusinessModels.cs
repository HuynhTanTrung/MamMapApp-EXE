using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class BusinessModels
    {
        public Guid BusinessModelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
