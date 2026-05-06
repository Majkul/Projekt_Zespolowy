using ProjektZespolowyGr3.Models.DbModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DomPogrzebowyProjekt.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public bool IsBanned { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public string? PhoneNumber { get; set; }

    }

}