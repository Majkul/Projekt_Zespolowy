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
            object? model;
            int totalClients = 0;
            switch (tab)
            {
                case "Users":
                    model = await GetFilteredUsersAsync(searchString, pageSize, pageNumber, "User");
                    totalClients = await GetUsersCountAsync(searchString, "Users", "User");
                    break;
                case "Admins":
                    model = await GetFilteredUsersAsync(searchString, pageSize, pageNumber, "Admin");
                    totalClients = await GetUsersCountAsync(searchString, "Admin", "Admin");
                    break;
                default:
                    model = null;
                    break;
            };



            ViewBag.SelectedTab = tab;
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentPageSize = pageSize;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalClients / (double)pageSize);

            return View(model);
        }

        private async Task<List<User>> GetFilteredUsersAsync(string? searchString, int pageSize, int pageNumber, string roleFilter)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(c =>
                    c.Id.ToString().Contains(searchString) ||
                    c.Username.Contains(searchString) ||
                    c.Email.Contains(searchString));
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
            return await users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        private async Task<int> GetUsersCountAsync(string? searchString, string tab, string roleFilter)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(c =>
                    c.Id.ToString().Contains(searchString) ||
                    c.Username.Contains(searchString) ||
                    c.Email.Contains(searchString));
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
            return await users.CountAsync();
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
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Address = user.Address,
                Longitude = user.Longitude,
                Latitude = user.Latitude,
                IsBanned = user.IsBanned,
                IsAdmin = user.IsAdmin,
                IsDeleted = user.IsDeleted,
                PhoneNumber = user.PhoneNumber
            };

            ViewBag.ModelId = id;
            return View(viewModel);


        }

        [HttpPost]
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

            user.IsBanned = u.IsBanned;
            user.IsAdmin = u.IsAdmin;
            user.IsDeleted = u.IsDeleted;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
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
