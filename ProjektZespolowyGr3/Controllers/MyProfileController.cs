using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.ViewModels;
using System.Security.Claims;
using ProjektZespolowyGr3.Models.System;
using System.Diagnostics;

namespace ProjektZespolowyGr3.Controllers
{
    [Authorize]
    public class MyProfileController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IGeocodingService _geocodingService;

        public MyProfileController(MyDBContext context, IGeocodingService geocodingService)
        {
            _context = context;
            _geocodingService = geocodingService;
        }

        // GET: MyProfile/Edit
        public async Task<IActionResult> Edit()
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

            var model = new EditMyProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email
            };

            return View(model);
        }

        // POST: MyProfile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditMyProfileViewModel model)
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
            else if (model.Address != user.Address) // geocoding tylko jesli zmieniono
            {
                user.Address = model.Address;
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

            // Email można zmienić tylko jeśli nie jest już zajęty przez innego użytkownika
            if (!string.IsNullOrEmpty(model.Email) && model.Email != user.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Ten adres email jest już zajęty.");
                    return View(model);
                }
                user.Email = model.Email;
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            await _context.SaveChangesAsync();

            TempData["AlertSuccess"] = "Profil został zaktualizowany.";
            return RedirectToAction(nameof(Edit));
        }

        // GET: MyProfile/Details
        public IActionResult Details()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            return RedirectToAction("Details", "UserProfile", new { id = userId });
        }
    }
}

