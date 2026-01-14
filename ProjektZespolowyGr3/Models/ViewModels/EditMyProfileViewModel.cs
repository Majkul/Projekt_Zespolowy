using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class EditMyProfileViewModel
    {
        [Display(Name = "Imię")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [Display(Name = "Nazwisko")]
        [StringLength(50)]
        public string? LastName { get; set; }

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
        public string Email { get; set; }
    }
}

