using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class UserPremiumPackage
    {
        public Guid UserId { get; set; }
        public int PremiumPackageId { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public AspNetUsers User { get; set; }
        public PremiumPackage PremiumPackage { get; set; }
    }

}
