using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.ViewModels;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers
{
    [Authorize]
    public class MyProfileController : Controller
    {
        private readonly MyDBContext _context;

        public MyProfileController(MyDBContext context)
        {
            _context = context;
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

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;

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

            await _context.SaveChangesAsync();

            TempData["AlertSuccess"] = "Profil został zaktualizowany.";
            return RedirectToAction(nameof(Edit));
        }

        // GET: MyProfile/Details
        public async Task<IActionResult> Details()
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

