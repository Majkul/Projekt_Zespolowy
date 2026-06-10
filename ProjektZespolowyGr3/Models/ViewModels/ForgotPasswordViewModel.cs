using System.ComponentModel.DataAnnotations;
using ProjektZespolowyGr3.Models;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany.")]
        [StringLength(MarketplaceLimits.MaxEmailLength, ErrorMessage = "Email może mieć maksymalnie 254 znaki.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres email.")]
        public string Email { get; set; } = string.Empty;
    }
}
