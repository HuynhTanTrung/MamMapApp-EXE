using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class PackageDescription
    {
        public int Id { get; set; }

        public int PremiumPackageId { get; set; }

        public string Description { get; set; } = default!;

        public PremiumPackage PremiumPackage { get; set; } = default!;
    }

}
