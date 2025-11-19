using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;

namespace ProjektZespolowyGr3.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MyDBContext _context;
    private readonly AuthService _authService;

    public HomeController(ILogger<HomeController> logger, MyDBContext context, AuthService authService)
    {
        _logger = logger;
        _context = context;
        _authService = authService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Route("Account/[action]")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (Request.Method == "GET")
            return View();
        else
        {
            if (_authService.Validate(model.Login, model.Password) != null)
            {
                var claims = _authService.GetClaims(_authService.GetUser(model.Login));
                var principal = new ClaimsPrincipal(claims);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewBag.Alert = "Błędny login lub hasło";
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
            Password = _authService.HashPassword(model.Password)
        };
        _context.UserAuths.Add(authUser);
        await _context.SaveChangesAsync();

        var claims = _authService.GetClaims(user);
        var principal = new ClaimsPrincipal(claims);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction(nameof(Index));
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
