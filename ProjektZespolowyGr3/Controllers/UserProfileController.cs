using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Controllers
{
    public class UserProfileController : Controller
    {
        private readonly MyDBContext _context;

        public UserProfileController(MyDBContext context)
        {
            _context = context;
        }

        // GET: Users/username
        [Route("Users/{username}")]
        public async Task<IActionResult> Details(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Listings)
                    .ThenInclude(l => l.Photos)
                        .ThenInclude(lp => lp.Upload)
                .Include(u => u.Listings)
                    .ThenInclude(l => l.Reviews)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            // Oblicz średnią ocenę na podstawie recenzji wszystkich ofert użytkownika
            var allReviews = user.Listings
                .SelectMany(l => l.Reviews)
                .ToList();

            var averageRating = allReviews.Any() 
                ? allReviews.Average(r => r.Rating) 
                : 0;

            var activeListings = user.Listings
                .Where(l => !l.IsSold && !l.IsArchived && l.StockQuantity > 0 && !l.IsPrivate)
                .ToList();

            ViewBag.AverageRating = Math.Round(averageRating, 2);
            ViewBag.ReviewCount = allReviews.Count;
            ViewBag.ActiveListings = activeListings;

            return View(user);
        }

        // GET: UserProfile/Details/5
        [Route("UserProfile/Details/{id:int}")]
        public async Task<IActionResult> DetailsById(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return RedirectToActionPermanent(nameof(Details), "UserProfile", new { username = user.Username });
        }
    }
}

