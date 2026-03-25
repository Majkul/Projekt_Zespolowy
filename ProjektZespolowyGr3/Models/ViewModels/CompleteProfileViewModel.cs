using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class CompleteProfileViewModel
    {
        [Display(Name = "Imię")]
        [Required(ErrorMessage = "Imię jest wymagane")]
        [StringLength(50, ErrorMessage = "Imię nie może być dłuższe niż 50 znaków")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Nazwisko")]
        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [StringLength(50, ErrorMessage = "Nazwisko nie może być dłuższe niż 50 znaków")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Adres")]
        [StringLength(200, ErrorMessage = "Adres nie może być dłuższy niż 200 znaków")]
        public string? Address { get; set; }

        [Display(Name = "Numer telefonu")]
        [StringLength(20, ErrorMessage = "Numer telefonu nie może być dłuższy niż 20 znaków")]
        [Phone(ErrorMessage = "Nieprawidłowy format numeru telefonu")]
        public string? PhoneNumber { get; set; }
    }
}

