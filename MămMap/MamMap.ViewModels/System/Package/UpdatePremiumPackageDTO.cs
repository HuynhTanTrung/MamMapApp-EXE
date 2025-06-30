using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Package
{
    public class UpdatePremiumPackageDTO
    {
        public string? Name { get; set; } = default!;
        public decimal? Price { get; set; }
        public List<string>? Descriptions { get; set; } = new();
    }
}
