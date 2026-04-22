using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> Index(
            string? searchString,
            string? location,
            string? type,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? selectedTagIds,
            string? sortBy)
        {
            selectedTagIds ??= new List<int>();

            IQueryable<Listing> query = _context.Listings
                .Where(l => !l.IsArchived)
                .Include(l => l.Photos).ThenInclude(lp => lp.Upload)
                .Include(l => l.Reviews)
                .Include(l => l.Tags).ThenInclude(lt => lt.Tag)
                .Include(l => l.Seller).ThenInclude(s => s.Listings).ThenInclude(sl => sl.Reviews);

            // Text search
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                query = query.Where(l =>
                    EF.Functions.ILike(l.Title, $"%{term}%") ||
                    (l.Description != null && EF.Functions.ILike(l.Description, $"%{term}%")));
            }

            // Location filter
            if (!string.IsNullOrWhiteSpace(location))
            {
                var locTerm = location.Trim();
                query = query.Where(l => l.Location != null && EF.Functions.ILike(l.Location, $"%{locTerm}%"));
            }

            // Type filter
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (type == "Sale")
                    query = query.Where(l => l.Type == ListingType.Sale);
                else if (type == "Trade")
                    query = query.Where(l => l.Type == ListingType.Trade);
            }

            // Price range (only meaningful for Sale listings)
            if (minPrice.HasValue)
                query = query.Where(l => l.Price == null || l.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(l => l.Price == null || l.Price <= maxPrice.Value);

            // Tag filter
            if (selectedTagIds.Any())
            {
                foreach (var tagId in selectedTagIds)
                    query = query.Where(l => l.Tags.Any(lt => lt.TagId == tagId));
            }

            // Sort
            query = sortBy switch
            {
                "price_asc"   => query.OrderBy(l => l.Price),
                "price_desc"  => query.OrderByDescending(l => l.Price),
                "oldest"      => query.OrderBy(l => l.CreatedAt),
                "most_viewed" => query.OrderByDescending(l => l.ViewCount),
                _             => query.OrderByDescending(l => l.CreatedAt),
            };

            var listings = await query.ToListAsync();

            var results = listings.Select(l => new BrowseListingsViewModel
            {
                Listing = l,
                ListingId = l.Id,
                Seller = l.Seller,
                SellerId = l.SellerId,
                PhotoUrl = l.Photos.FirstOrDefault(lp => lp.IsFeatured)?.Upload.Url,
                AverageRating = l.Seller.Listings.SelectMany(sl => sl.Reviews).Any()
                    ? l.Seller.Listings.SelectMany(sl => sl.Reviews).Average(r => r.Rating)
                    : 0,
                ReviewCount = l.Seller.Listings.SelectMany(sl => sl.Reviews).Count(),
            }).ToList();

            var filterModel = new ListingsFilterViewModel
            {
                SearchString = searchString,
                Location = location,
                Type = type,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SelectedTagIds = selectedTagIds,
                SortBy = sortBy,
                AvailableTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync(),
                FeaturedResults = results.Where(r => r.Listing.IsFeatured),
                Results = results.Where(r => !r.Listing.IsFeatured),
            };

            return View(filterModel);
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
                .Include(l => l.ExchangeAcceptedTags)
                    .ThenInclude(et => et.Tag)
                .Include(l => l.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .Include(l => l.Reviews)
                    .ThenInclude(r => r.Photos)
                        .ThenInclude(rp => rp.Upload)
                .Include(l => l.ShippingOptions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing == null)
            {
                return NotFound();
            }
            // Increment view count for everyone except the listing's own seller
            var viewerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isSeller = viewerIdClaim != null && viewerIdClaim == listing.SellerId.ToString();
            if (!isSeller)
            {
                await _context.Listings
                    .Where(l => l.Id == listing.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.ViewCount, l => l.ViewCount + 1));
                listing.ViewCount++;
            }
            return View(listing);
        }

        // GET: Listings/Create
        [Authorize]
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateListingViewModel model)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId) || currentUserId <= 0)
            {
                return Challenge();
            }

            // cena potzebna jesli "nie do wymiany"
            if (model.NotExchangeable && (!model.Price.HasValue || model.Price.Value <= 0))
            {
                ModelState.AddModelError("Price", "Price must be greater than zero.");
            }

            if (model.StockQuantity < 1)
                ModelState.AddModelError(nameof(CreateListingViewModel.StockQuantity), "Liczba sztuk musi być co najmniej 1.");

            if (!ModelState.IsValid)
            {
                _helper.PopulateAvailableTags(model);
                return View(model);
            }

            var photoFiles = model.PhotoFiles ?? new List<IFormFile>();
            if (photoFiles.Count > 5)
            {
                ModelState.AddModelError("PhotoFiles", "You can upload a maximum of 5 photos.");
                return View(model);
            }

            var listing = new Listing
            {
                Title = model.Title,
                Description = model.Description,
                Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim(),
                Type = model.Type,
                Price = model.Type == ListingType.Trade ? null : model.Price,
                StockQuantity = model.StockQuantity,
                SellerId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                NotExchangeable = model.Type == ListingType.Trade ? false : model.NotExchangeable,
                MinExchangeValue = model.Type == ListingType.Trade ? null : model.MinExchangeValue,
                ExchangeDescription = string.IsNullOrWhiteSpace(model.ExchangeDescription) ? null : model.ExchangeDescription.Trim()
            };
            ListingStockHelper.SyncSoldFlag(listing);

            foreach (var opt in model.ShippingOptions.Where(o => !string.IsNullOrWhiteSpace(o.Name)))
            {
                listing.ShippingOptions.Add(new ListingShippingOption
                {
                    Name = opt.Name.Trim(),
                    Price = Math.Max(0, opt.Price)
                });
            }

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

            if (model.SelectedExchangeAcceptedTagIds != null && model.SelectedExchangeAcceptedTagIds.Any())
            {
                foreach (var tagId in model.SelectedExchangeAcceptedTagIds.Distinct())
                {
                    var tag = _context.Tags.Find(tagId);
                    if (tag != null)
                    {
                        listing.ExchangeAcceptedTags.Add(new ListingExchangeAcceptedTag
                        {
                            TagId = tag.Id,
                            Listing = listing
                        });
                    }
                }
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            if (photoFiles.Count != 0)
            {
                foreach (var file in photoFiles)
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

                foreach (var file in photoFiles)
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
                        UploaderId = currentUserId,
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
                //_context.Listings.Remove(listing);
                listing.IsArchived = true;
            }

            //await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.Id == id);
        }
    }
}
