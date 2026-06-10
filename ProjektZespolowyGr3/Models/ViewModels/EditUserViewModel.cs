using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DomPogrzebowyProjekt.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Username { get; set; } = string.Empty;
        [Required(ErrorMessage = "Imię jest wymagane.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        [StringLength(MarketplaceLimits.MaxEmailLength, ErrorMessage = "Email może mieć maksymalnie 254 znaki.")]
        public string? Email { get; set; }
        [StringLength(MarketplaceLimits.MaxProfileDescriptionLength, ErrorMessage = "Opis profilu może mieć maksymalnie 1000 znaków.")]
        public string? ProfileDescription { get; set; }
        public string? Address { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public bool IsBanned { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public string? PhoneNumber { get; set; }

    }

}