using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;
using DomPogrzebowyProjekt.Models.System;
using Microsoft.EntityFrameworkCore;

namespace ProjektZespolowyGr3.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailService _emailService;
    private readonly MyDBContext _context;
    private readonly AuthService _authService;
    private readonly IGeocodingService _geocodingService;

    public HomeController(ILogger<HomeController> logger, MyDBContext context, AuthService authService, IEmailService emailService, IGeocodingService geocodingService)
    {
        _logger = logger;
        _context = context;
        _authService = authService;
        _emailService = emailService;
        _geocodingService = geocodingService;
    }

    public async Task<IActionResult> Index()
    {
        // Pobierz kilka najnowszych aktywnych ofert do wyświetlenia na stronie głównej
        var latestListings = await _context.Listings
            .Where(l => !l.IsSold && !l.IsArchived && !l.IsPrivate && l.StockQuantity > 0)
            .Include(l => l.Photos)
                .ThenInclude(lp => lp.Upload)
            .Include(l => l.Seller)
            .OrderByDescending(l => l.CreatedAt)
            .Take(8)
            .ToListAsync();

        ViewBag.LatestListings = latestListings;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Route("Account/[action]")]
    public async Task<IActionResult> Login(string login, string password, string? returnUrl = null)
    {
        if (Request.Method == "GET")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        else
        {

            if (_authService.Validate(login, password) != null)
            {
                var user = _authService.GetUser(login);
                if (user == null)
                {
                    TempData["AlertErrorLogin"] = "Incorrect login or password";
                    return View();
                }

                var userAuth = _context.UserAuths.FirstOrDefault(x => x.UserId == user.Id);
                if (userAuth is not { EmailVerified: true })
                {
                    //ModelState.AddModelError("", "You need to confirm your email address.");
                    TempData["AlertErrorLogin"] = "You need to confirm your email address.";
                    return View();
                }
                if (user.IsDeleted || user.IsBanned)
                {
                    //ModelState.AddModelError("", "Your account is not active.");
                    TempData["AlertErrorLogin"] = "Your account is not active.";
                    return View();
                }
                var claims = _authService.GetClaims(user);
                var principal = new ClaimsPrincipal(claims);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["AlertErrorLogin"] = "Incorrect login or password";
                return View();
            }
        }
    }

    [Route("Account/[action]")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [Route("Account/[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Hasła nie są takie same.");
            return View(model);
        }

        if (_authService.UserExists(model.Login))
        {
            ModelState.AddModelError(nameof(model.Login), "Ten login jest już zajęty.");
            return View(model);
        }

        if (_authService.EmailTaken(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Ten email jest już zajęty.");
            return View(model);
        }

        string token = Guid.NewGuid().ToString();
        var salt = _authService.GenerateSalt();

        var user = new Models.DbModels.User
        {
            Username = model.Login,
            Email = model.Email,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var authUser = new UserAuth
        {
            UserId = user.Id,
            Password = _authService.HashPassword(model.Password, salt),
            PasswordSalt = salt,
            EmailVerified = false,
            EmailVerificationToken = token
        };
        _context.UserAuths.Add(authUser);
        await _context.SaveChangesAsync();

        var verifyUrl = Url.Action(
            "VerifyEmail",
            "Account",
            new { email = user.Email, token = token },
            Request.Scheme);

        string body = $@"
        <p>Thank you for registering.</p>
        <p>Please click the link below to confirm your email address:</p>
        <p><a href='{verifyUrl}'>Confirm your email address</a></p>";

        await _emailService.SendEmailAsync(user.Email, "Confirm your email address", body);


        return RedirectToAction("RegisterConfirmation");
    }

    [Route("Account/[action]")]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [Route("Account/[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user != null)
        {
            var auth = await _context.UserAuths.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (auth != null)
            {
                auth.PasswordResetToken = Guid.NewGuid().ToString("N");
                auth.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                var resetUrl = Url.Action("ResetPassword", "Account", new { email = user.Email, token = auth.PasswordResetToken }, Request.Scheme);
                var body = $@"<p>Otrzymaliśmy prośbę o reset hasła.</p><p><a href='{resetUrl}'>Ustaw nowe hasło</a></p>";
                await _emailService.SendEmailAsync(user.Email, "Reset hasła", body);
            }
        }

        TempData["AlertSuccess"] = "Jeśli konto istnieje, wysłaliśmy link do resetu hasła.";
        return RedirectToAction(nameof(Login));
    }

    [Route("Account/[action]")]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Nieprawidłowy link resetu hasła.");
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [Route("Account/[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Nieprawidłowy link resetu hasła.");
            return View(model);
        }

        var auth = await _context.UserAuths.FirstOrDefaultAsync(a => a.UserId == user.Id);
        if (auth == null ||
            auth.PasswordResetToken != model.Token ||
            auth.PasswordResetTokenExpiresAt == null ||
            auth.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            ModelState.AddModelError(string.Empty, "Link resetu hasła wygasł albo jest nieprawidłowy.");
            return View(model);
        }

        var salt = _authService.GenerateSalt();
        auth.PasswordSalt = salt;
        auth.Password = _authService.HashPassword(model.Password, salt);
        auth.PasswordResetToken = null;
        auth.PasswordResetTokenExpiresAt = null;
        await _context.SaveChangesAsync();

        TempData["AlertSuccess"] = "Hasło zostało zmienione. Możesz się zalogować.";
        return RedirectToAction(nameof(Login));
    }

    [Route("Account/[action]")]
    public async Task<IActionResult> VerifyEmail(string email, string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return BadRequest("Incorrect activation link.");

        var userAuth = await _context.UserAuths.FirstOrDefaultAsync(ua => ua.UserId == user.Id);
        if (userAuth == null || userAuth.EmailVerificationToken != token)
            return BadRequest("Incorrect activation link.");

        userAuth.EmailVerified = true;
        userAuth.EmailVerificationToken = null;

        await _context.SaveChangesAsync();

        // Automatyczne logowanie po weryfikacji maila
        var claims = _authService.GetClaims(user);
        var principal = new ClaimsPrincipal(claims);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Sprawdź czy profil jest uzupełniony (imię i nazwisko są wymagane)
        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
        {
            TempData["AlertSuccess"] = "Email został potwierdzony. Uzupełnij swój profil.";
            return RedirectToAction("CompleteProfile");
        }

        TempData["AlertSuccess"] = "Email został potwierdzony. Witamy!";
        return RedirectToAction("Index");
    }
    [Route("Account/[action]")]
    public IActionResult RegisterConfirmation()
    {
        return View();
    }

    [Authorize]
    [Route("Account/[action]")]
    public async Task<IActionResult> CompleteProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Jeśli profil już jest uzupełniony, przekieruj do strony głównej
        if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
        {
            return RedirectToAction("Index");
        }

        var model = new CompleteProfileViewModel
        {
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Address = user.Address,
            PhoneNumber = user.PhoneNumber
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [Route("Account/[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(model.Address))
        {
            user.Longitude = null;
            user.Latitude = null;
        }
        else
        {
            var location = await _geocodingService.GetAddressLocation(model.Address);

            if (location.HasValue)
            {
                user.Longitude = location.Value.Longitude;
                user.Latitude = location.Value.Latitude;
            }
            else
            {
                ModelState.AddModelError("Address", "Nie można znaleźć lokalizacji dla podanego adresu.");
                return View(model);
            }
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Address = model.Address;
        user.PhoneNumber = model.PhoneNumber;

        await _context.SaveChangesAsync();

        TempData["AlertSuccess"] = "Profil został uzupełniony. Witamy!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Route("Account/[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogOut(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}