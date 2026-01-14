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

    public HomeController(ILogger<HomeController> logger, MyDBContext context, AuthService authService, IEmailService emailService)
    {
        _logger = logger;
        _context = context;
        _authService = authService;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        // Pobierz kilka najnowszych aktywnych ofert do wyświetlenia na stronie głównej
        var latestListings = await _context.Listings
            .Where(l => !l.IsSold)
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
    public async Task<IActionResult> Login(string login, string password, string returnUrl = null)
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
                var userAuth = _context.UserAuths.FirstOrDefault(x => x.UserId == user.Id);
                if (!userAuth.EmailVerified)
                {
                    ModelState.AddModelError("", "You need to confirm your email address.");
                    TempData["AlertError"] = "You need to confirm your email address.";
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
                TempData["AlertError"] = "Incorrect login or password";
                return View();
            }
        }
    }

    [Route("Account/[action]")]
    public async Task<IActionResult> Register()
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
            ViewBag.Alert = "Passwords do not match";
            return View(model);
        }

        if (_authService.UserExists(model.Login))
        {
            ViewBag.Alert = "User already exists";
            return View(model);
        }

        if (_authService.EmailTaken(model.Email))
        {
            TempData["AlertError"] = "User with this email already exists";
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