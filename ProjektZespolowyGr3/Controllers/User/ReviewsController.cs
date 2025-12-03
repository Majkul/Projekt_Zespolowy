using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;

namespace ProjektZespolowyGr3.Controllers.User
{
    public class ReviewsController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly HelperService _helper;

        public ReviewsController(MyDBContext context, IWebHostEnvironment env, HelperService helper)
        {
            _context = context;
            _env = env;
            _helper = helper;
        }

        // GET: Reviews
        public async Task<IActionResult> Index()
        {
            var myDBContext = _context.Review.Include(r => r.Listing).Include(r => r.Reviewer);
            return View(await myDBContext.ToListAsync());
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Review
                .Include(r => r.Listing)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // GET: Reviews/Create
        public IActionResult Create()
        {
            ViewData["ListingId"] = new SelectList(_context.Listings, "Id", "Id");
            ViewData["ReviewerId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // GET: Reviews/Create?listingId=5
        [HttpGet]
        public IActionResult Create(int listingId)
        {
            var listing = _context.Listings.FirstOrDefault(l => l.Id == listingId);

            if (listing == null)
                return NotFound();

            var model = new CreateReviewViewModel
            {
                ListingId = listingId
            };

            return View(model);
        }

        // POST: Reviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            // ZMIENIC TODO tylko ogloszenia ktore zakupil i ogolnie nieprototypowy review system

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.PhotoFiles.Count > 5)
            {
                ModelState.AddModelError("PhotoFiles", "You can upload a maximum of 5 photos.");
                return View(model);
            }

            // ZMIENIC POTEM
            var userId = _helper.GetCurrentUserId();

            var listing = _context.Listings.FirstOrDefault(l => l.Id == model.ListingId);

            if (listing == null)
            {
                ModelState.AddModelError("", "Listing not found.");
                return View(model);
            }

            // czy uzytkownik juz dodal recenzje do ogloszenia
            var existingReview = _context.Reviews.FirstOrDefault(r => r.ListingId == model.ListingId && r.ReviewerId == userId);

            if (existingReview != null)
            {
                ModelState.AddModelError("", "You have already reviewed this listing.");
                return View(model);
            }

            // czy nie jestes wlascicielem zgloszenia
            if (listing.SellerId == userId)
            {
                ModelState.AddModelError("", "You cannot review your own listing.");
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

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            if (model.PhotoFiles != null && model.PhotoFiles.Count != 0)
            {
                foreach (var file in model.PhotoFiles)
                {
                    if (file.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("PhotoFiles", "Each photo must be less than 5 MB.");
                        return View(model);
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("PhotoFiles", "Only .jpg, .jpeg, .png files are allowed.");
                        return View(model);
                    }

                    if (!file.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("PhotoFiles", "Invalid file type.");
                        return View(model);
                    }
                }

                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                foreach (var file in model.PhotoFiles)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var upload = new Upload
                    {
                        FileName = Path.GetFileName(file.FileName),
                        Extension = ext,
                        Url = $"/uploads/{fileName}",
                        SizeBytes = file.Length,
                        UploaderId = userId,
                        UploadedAt = DateTime.UtcNow
                    };
                    _context.Uploads.Add(upload);

                    var reviewPhoto = new ReviewPhoto
                    {
                        Review = review,
                        Upload = upload
                    };

                    review.Photos.Add(reviewPhoto);
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

            var review = await _context.Review.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }
            ViewData["ListingId"] = new SelectList(_context.Listings, "Id", "Id", review.ListingId);
            ViewData["ReviewerId"] = new SelectList(_context.Users, "Id", "Id", review.ReviewerId);
            return View(review);
        }

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

        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Review
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
            var review = await _context.Review.FindAsync(id);
            if (review != null)
            {
                _context.Review.Remove(review);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
            return _context.Review.Any(e => e.Id == id);
        }
    }
}
