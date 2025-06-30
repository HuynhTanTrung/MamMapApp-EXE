using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.Entities
{
    public class UserOtp
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Otp { get; set; }
        public DateTime ExpiryTime { get; set; }
        public AspNetUsers User { get; set; }
    }
}
