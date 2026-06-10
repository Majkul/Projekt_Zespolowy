using System.ComponentModel.DataAnnotations;
using ProjektZespolowyGr3.Models;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class EditMyProfileViewModel
    {
        [Display(Name = "Imię")]
        [Required(ErrorMessage = "Imię jest wymagane.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Nazwisko")]
        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Opis profilu")]
        [StringLength(MarketplaceLimits.MaxProfileDescriptionLength, ErrorMessage = "Opis profilu może mieć maksymalnie 1000 znaków.")]
        public string? ProfileDescription { get; set; }

        [Display(Name = "Adres")]
        [StringLength(200)]
        public string? Address { get; set; }

        [Display(Name = "Numer telefonu")]
        [StringLength(20)]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        [Required(ErrorMessage = "Email jest wymagany")]
        [StringLength(MarketplaceLimits.MaxEmailLength, ErrorMessage = "Email może mieć maksymalnie 254 znaki.")]
        public string Email { get; set; } = string.Empty;
    }
}

