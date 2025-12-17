using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.System
{
    public class AuthService
    {
        private readonly MyDBContext _context;

        public AuthService(MyDBContext context)
        {
            _context = context;
        }

        public ClaimsIdentity GetClaims(User user)
        {
            var result = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            result.AddClaim(new Claim(ClaimTypes.Name, user.Username));
            result.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            if (user.IsAdmin)
            {
                result.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
            }
            else
            {
                result.AddClaim(new Claim(ClaimTypes.Role, "Client"));
            }
            result.AddClaim(new Claim("Login", user.Username ?? string.Empty));
            return result;
        }

        internal User GetUser(string login)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == login);

            if (user == null)
                return null;

            return user;
        }

        public User? Validate(string login, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Username == login);
            var userAuth = _context.UserAuths.FirstOrDefault(x => x.UserId == user.Id);
            if (user != null && VerifyPassword(password, userAuth.PasswordSalt, userAuth.Password))
            {
                return user;
            }
            return null;
        }

        public bool UserExists(string login)
        {
            return _context.Users.Any(u => u.Username == login);
        }

        public bool EmailTaken(string email)
        {
            return _context.Users.Any(u => u.Email == email);
        }

        public string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        private bool VerifyPassword(string inputPassword, string salt, string storedHash)
        {
            var inputHash = HashPassword(inputPassword, salt);
            return inputHash == storedHash;
        }
        public async Task LogOut(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

    }
}

