using System.ComponentModel.DataAnnotations;

namespace HUK_Part_2_.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [MinLength(2, ErrorMessage = "Username can be min 2 characters.")]
        [MaxLength(30, ErrorMessage = "Username can be max 30 characters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password can be min 6 characters.")]
        [MaxLength(18, ErrorMessage = "Password can be max 18 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Re-Password is required.")]
        [MinLength(6, ErrorMessage = "Re-Password can be min 6 characters.")]
        [MaxLength(18, ErrorMessage = "Re-Password can be max 18 characters.")]
        [Compare(nameof(Password))]
        public string RePassword { get; set; }
    }
}
