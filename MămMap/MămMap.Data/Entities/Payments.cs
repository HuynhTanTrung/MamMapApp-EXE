using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class Payments
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public int PremiumPackageId { get; set; }

        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentCode { get; set; }
        public bool PaymentStatus { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public AspNetUsers User { get; set; } = null!;
        public PremiumPackage PremiumPackage { get; set; } = null!;
    }
}
