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

        // GET: UserProfile/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Listings)
                    .ThenInclude(l => l.Photos)
                        .ThenInclude(lp => lp.Upload)
                .Include(u => u.Listings)
                    .ThenInclude(l => l.Reviews)
                .FirstOrDefaultAsync(u => u.Id == id);

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
                .Where(l => !l.IsSold)
                .ToList();

            ViewBag.AverageRating = Math.Round(averageRating, 2);
            ViewBag.ReviewCount = allReviews.Count;
            ViewBag.ActiveListings = activeListings;

            return View(user);
        }
    }
}

