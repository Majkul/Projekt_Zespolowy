using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IFileService _fileService;

        public ReviewsController(MyDBContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        private int GetCurrentUserId()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idString) || !int.TryParse(idString, out var id))
            {
                throw new Exception("Brak zalogowanego użytkownika");
            }
            return id;
        }

        // GET: Reviews
        public async Task<IActionResult> Index()
        {
            var myDBContext = _context.Reviews.Include(r => r.Listing).Include(r => r.Reviewer);
            return View(await myDBContext.ToListAsync());
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Listing)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }
        [Authorize]
        // GET: Reviews/Create
        public IActionResult Create()
        {
            ViewData["ListingId"] = new SelectList(_context.Listings, "Id", "Id");
            ViewData["ReviewerId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }
        [Authorize]
        // GET: Reviews/Create?listingId=5
        [HttpGet]
        public IActionResult Create(int listingId)
        {
            var listing = _context.Listings.FirstOrDefault(l => l.Id == listingId);

            if (listing == null)
                return NotFound();

            // wlasne
            var userId = GetCurrentUserId();
            if (listing.SellerId == userId)
            {
                return BadRequest("Nie możesz oceniać własnego ogłoszenia.");
            }

            var model = new CreateReviewViewModel
            {
                ListingId = listingId
            };

            return View(model);
        }
        [Authorize]
        // POST: Reviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            // ZMIENIC TODO tylko ogloszenia ktore zakupil i ogolnie nieprototypowy review system

            foreach (var (field, message) in _fileService.ValidateImages(model.PhotoFiles, maxCount: 5))
                ModelState.AddModelError(field, message);

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            var listing = _context.Listings.FirstOrDefault(l => l.Id == model.ListingId);
            if (listing == null)
            {
                ModelState.AddModelError("", "Ogłoszenie nie zostało znalezione.");
                return View(model);
            }

            if (_context.Reviews.Any(r => r.ListingId == model.ListingId && r.ReviewerId == userId))
            {
                ModelState.AddModelError("", "Już oceniłeś to ogłoszenie.");
                return View(model);
            }

            if (listing.SellerId == userId)
            {
                ModelState.AddModelError("", "Nie możesz oceniać własnego ogłoszenia.");
                return View(model);
            }

            var review = new Review
            {
                ListingId = model.ListingId,
                Listing = listing,
                ReviewerId = userId,
                Rating = model.Rating,
                Description = model.Description,
                CreatedAt = DateTime.UtcNow
            };

            if (model.PhotoFiles?.Count > 0)
            {
                foreach (var file in model.PhotoFiles)
                {
                    var upload = await _fileService.SaveFileAsync(file, userId);
                    review.Photos.Add(new ReviewPhoto { Review = review, Upload = upload });
                }
            }

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Listings", new { id = listing.Id });
        }

        // GET: Reviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }
            ViewData["ListingId"] = new SelectList(_context.Listings, "Id", "Id", review.ListingId);
            ViewData["ReviewerId"] = new SelectList(_context.Users, "Id", "Id", review.ReviewerId);
            return View(review);
        }
        [Authorize]
        // POST: Reviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ListingId,Rating,ReviewerId,Description,CreatedAt,Upvotes,Downvotes")] Review review)
        {
            if (id != review.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(review);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ListingId"] = new SelectList(_context.Listings, "Id", "Id", review.ListingId);
            ViewData["ReviewerId"] = new SelectList(_context.Users, "Id", "Id", review.ReviewerId);
            return View(review);
        }
        [Authorize]
        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Listing)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }
    }
}
