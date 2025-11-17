using ProjektZespolowyGr3.Models.DbModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjektZespolowyGr3.Models
{
    public class UserAuth
    {
        [Required, Key]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
    }
}