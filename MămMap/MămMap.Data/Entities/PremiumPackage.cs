using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class PremiumPackage
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public bool Status { get; set; } = true;
        public ICollection<PackageDescription> Descriptions { get; set; } = new List<PackageDescription>();
        public ICollection<UserPremiumPackage> UserPremiumPackages { get; set; }

    }
}
