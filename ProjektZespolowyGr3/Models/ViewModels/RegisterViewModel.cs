using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Login jest wymagany.")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
        public string ConfirmPassword { get; set; }
    }
}
