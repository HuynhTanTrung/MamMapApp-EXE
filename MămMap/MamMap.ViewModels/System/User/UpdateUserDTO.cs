namespace MamMapApp.Models
{
    public class UpdateUserDTO
    {
        public Guid Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Fullname { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? Image { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
    }

}
