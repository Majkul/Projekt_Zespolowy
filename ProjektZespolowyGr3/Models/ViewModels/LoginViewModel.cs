<<<<<<< HEAD
﻿using System.ComponentModel.DataAnnotations;

=======
>>>>>>> origin/main
namespace ProjektZespolowyGr3.Models.ViewModels
{

    public class LoginViewModel
    {
<<<<<<< HEAD
        [Required(ErrorMessage = "Login jest wymagany.")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        public string Password { get; set; }
=======
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
>>>>>>> origin/main
    }
}
