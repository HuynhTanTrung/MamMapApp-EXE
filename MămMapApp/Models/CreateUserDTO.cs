using System.ComponentModel.DataAnnotations;

namespace MamMapApp.Models
{
    public class CreateUserDTO
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        [MinLength(8, ErrorMessage = "Mật khẩu phải dài ít nhất 6 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$",
            ErrorMessage = "Mật khẩu phải bao gồm ít nhất một chữ cái viết hoa, một chữ cái viết thường, một số và một ký tự đặc biệt.")]
        public string Password { get; set; } = string.Empty;
    }
}
