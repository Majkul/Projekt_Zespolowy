using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;
using ProjektZespolowyGr3.Controllers;
using ProjektZespolowyGr3.Models.System;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers.User
{
    public class ListingsController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuthService _auth;
        private readonly HelperService _helper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ListingsController(MyDBContext context, IWebHostEnvironment env, AuthService auth, HelperService helper, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _env = env;
            _auth = auth;
            _helper = helper;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: Listings
        public async Task<IActionResult> Index()
        {
            var listings = await _context.Listings
                .Include(l => l.Photos)
                    .ThenInclude(lp => lp.Upload)
                .Include(l => l.Reviews)
                .Include(l => l.Seller)
                    .ThenInclude(s => s.Listings)
                        .ThenInclude(sl => sl.Reviews)
                .ToListAsync();

            var model = listings.Select(l => new BrowseListingsViewModel
            {
                Listing = l,
                ListingId = l.Id,
                Seller = l.Seller,
                SellerId = l.SellerId,
                PhotoUrl = l.Photos.FirstOrDefault(lp => lp.IsFeatured)?.Upload.Url,
                AverageRating = l.Seller.Listings
                        .SelectMany(sl => sl.Reviews)
                        .Any()
                        ? l.Seller.Listings.SelectMany(sl => sl.Reviews).Average(r => r.Rating)
                        : 0,
                ReviewCount = l.Seller.Listings.SelectMany(sl => sl.Reviews).Count(),
            }).ToList();

            return View(model);
        }

        // GET: Listings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.Seller)
                .Include(l => l.Photos)
                    .ThenInclude(lp => lp.Upload)
                .Include(l => l.Tags)
                    .ThenInclude(lt => lt.Tag)
                .Include(l => l.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .Include(l => l.Reviews)
                    .ThenInclude(r => r.Photos)
                        .ThenInclude(rp => rp.Upload)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }

        // GET: Listings/Create
        [HttpGet]
        public IActionResult Create()
        {
            // TODO: usunac
            _helper.MakeSomeTags();
            var model = new CreateListingViewModel
            {
                AvailableTags = _context.Tags
                    .OrderBy(t => t.Name)
                    .Select(t => new SelectListItem
                    {
                        Text = t.Name,
                        Value = t.Id.ToString()
                    })
                    .ToList()
            };
            return View(model);
        }

        // POST: Listings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateListingViewModel model)
        {
            // cena tylko dla sprzedazy
            if (model.Type == ListingType.Sale && (!model.Price.HasValue || model.Price <= 0))
            {
                ModelState.AddModelError("Price", "Price must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                _helper.PopulateAvailableTags(model);
                return View(model);
            }

            if (model.PhotoFiles.Count > 5)
            {
                ModelState.AddModelError("PhotoFiles", "You can upload a maximum of 5 photos.");
                return View(model);
            }

            // ZMIENIC POTEM
            //var userId = _helper.GetCurrentUserId();
            // To wersja już chyba zmieniona \/
            int userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out userId);
            }


            var listing = new Listing
            {
                Title = model.Title,
                Description = model.Description,
                Type = model.Type,
                Price = model.Type == ListingType.Trade ? null : model.Price,
                SellerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (model.SelectedTagIds != null && model.SelectedTagIds.Any())
            {
                foreach (var tagId in model.SelectedTagIds)
                {
                    var tag = _context.Tags.Find(tagId);
                    if (tag != null)
                    {
                        listing.Tags.Add(new ListingTag
                        {
                            TagId = tag.Id,
                            Listing = listing
                        });
                    }
                }
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            if (model.PhotoFiles != null && model.PhotoFiles.Count != 0)
            {
                foreach (var file in model.PhotoFiles)
                {
                    if (file.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("PhotoFiles", "Each photo must be less than 5 MB.");
                        _helper.PopulateAvailableTags(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("PhotoFiles", "Only .jpg, .jpeg, .png files are allowed.");
                        _helper.PopulateAvailableTags(model);
                        return View(model);
                    }

                    if (!file.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("PhotoFiles", "Invalid file type.");
                        return View(model);
                    }
                }

                bool first = true; // pierwsze staje sie featured

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

                    var listingPhoto = new ListingPhoto
                    {
                        Listing = listing,
                        Upload = upload,
                        IsFeatured = first
                    };

                    listing.Photos.Add(listingPhoto);

                    first = false;
                }
            }

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = listing.Id });
        }

        // GET: Listings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null)
            {
                return NotFound();
            }
            ViewData["SellerId"] = new SelectList(_context.Users, "Id", "Id", listing.SellerId);
            return View(listing);
        }

        // POST: Listings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,SellerId,Type,Photos,Tags,IsFeatured,CreatedAt,UpdatedAt,Price")] Listing listing)
        {
            if (id != listing.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(listing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ListingExists(listing.Id))
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
            ViewData["SellerId"] = new SelectList(_context.Users, "Id", "Id", listing.SellerId);
            return View(listing);
        }

        // GET: Listings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _context.Listings
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }

        // POST: Listings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing != null)
            {
                _context.Listings.Remove(listing);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.Id == id);
        }
    }
}
