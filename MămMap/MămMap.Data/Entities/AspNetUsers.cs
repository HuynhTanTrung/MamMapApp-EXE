using Microsoft.AspNetCore.Identity;

namespace MamMap.Data.Entities
{
    public class AspNetUsers : IdentityUser<Guid>
    {
        public string Fullname { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Image { get; set; }
        public bool Status { get; set; } = true;
        public ICollection<UserPremiumPackage> UserPremiumPackages { get; set; }
    }
}
