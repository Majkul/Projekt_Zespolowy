using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;
using ProjektZespolowyGr3.Models.DbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DomPogrzebowyProjekt.Models.ViewModels;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Net;
using System.Runtime.CompilerServices;

namespace DomPogrzebowyProjekt.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserManageController : Controller
    {
        private readonly MyDBContext _context;

        public UserManageController(MyDBContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string tab = "Users", string? searchString = null, int pageSize = 25, int pageNumber = 1)
        {
            var selectedTab = tab == "Admins" ? "Admins" : "Users";
            var roleFilter = selectedTab == "Admins" ? "Admin" : "User";
            object? model = await GetFilteredUsersAsync(searchString, pageSize, roleFilter);

            ViewBag.SelectedTab = selectedTab;
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentPageSize = pageSize;

            return View(model);
        }

        private IQueryable<User> BuildUserManageQuery(string? searchString, string roleFilter)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                var loweredTerm = term.ToLower();
                users = users.Where(u =>
                    u.Id.ToString().Contains(term) ||
                    u.Username.ToLower().Contains(loweredTerm) ||
                    u.Email.ToLower().Contains(loweredTerm));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            { 
                switch (roleFilter)
                {
                    case "User":
                        users = users.Where(u => u.IsAdmin == false);
                        break;
                    case "Admin":
                        users = users.Where(u => u.IsAdmin == true);
                        break;
                }
            }

            return users;
        }

        private async Task<List<User>> GetFilteredUsersAsync(string? searchString, int pageSize, string roleFilter)
        {
            // Stronicowanie odbywa się po stronie przeglądarki (admin-pagination.js),
            // dlatego z bazy pobieramy wszystkie pasujące rekordy.
            return await BuildUserManageQuery(searchString, roleFilter)
                .OrderBy(u => u.Id)
                .ToListAsync();
        }
        private async Task<int> GetUsersCountAsync(string? searchString, string tab, string roleFilter)
        {
            return await BuildUserManageQuery(searchString, roleFilter).CountAsync();
        }

        //---User---
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Username = user.Username,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email,
                Address = user.Address,
                Longitude = user.Longitude,
                Latitude = user.Latitude,
                IsBanned = user.IsBanned,
                IsAdmin = user.IsAdmin,
                IsDeleted = user.IsDeleted,
                PhoneNumber = user.PhoneNumber,
                ProfileDescription = user.ProfileDescription
            };

            ViewBag.ModelId = id;
            return View(viewModel);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, EditUserViewModel u)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ModelId = id;
                return View(u);
            }

            if (u.Username != null)
                user.Username = u.Username;
            if (u.FirstName != null)
                user.FirstName = u.FirstName;
            if (u.LastName != null)
                user.LastName = u.LastName;
            if (u.Email != null)
                user.Email = u.Email;
            if (u.Address != null)
                user.Address = u.Address;
            if (u.Longitude != null)
                user.Longitude = u.Longitude;
            if (u.Latitude != null)
                user.Latitude = u.Latitude;
            if (u.PhoneNumber != null)
                user.PhoneNumber = u.PhoneNumber;
            user.ProfileDescription = string.IsNullOrWhiteSpace(u.ProfileDescription) ? null : u.ProfileDescription.Trim();

            user.IsBanned = u.IsBanned;
            user.IsAdmin = u.IsAdmin;
            user.IsDeleted = u.IsDeleted;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            //TODO obsługa zarchiwizowanych wiadomości w chacie (wyszarzyć je czy coś)
            //Zablokowac wyświetlanie zarchiwizowanych ofert
            //Zbanować wyświetlanie strony dla usuniętych użytonikwów - oznaczyć jakoś żeby dał się zrobić konto na tego maila znowu, np usunąć maila tam?
            _context.Messages.Where(lp => lp.SenderId == id || lp.ReceiverId == id)
                .ToList().ForEach(lp =>
                {
                    lp.IsArchived = true;
                });
            _context.Listings.Where(lp => lp.SellerId == id)
                .ToList().ForEach(lp =>
                {
                    lp.IsArchived = true;
                });
            user.IsDeleted = true;
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
