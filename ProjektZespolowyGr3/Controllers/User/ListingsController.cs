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
        private readonly IFileService _fileService;
        private readonly AuthService _auth;
        private readonly HelperService _helper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGeocodingService _geocodingService;

<<<<<<< HEAD
        public ListingsController(MyDBContext context, IFileService fileService, AuthService auth, HelperService helper, IHttpContextAccessor httpContextAccessor)
=======
        public ListingsController(MyDBContext context, IFileService fileService, AuthService auth, HelperService helper, IHttpContextAccessor httpContextAccessor, IGeocodingService geocodingService)
>>>>>>> origin/main
        {
            _context = context;
            _fileService = fileService;
            _auth = auth;
            _helper = helper;
            _httpContextAccessor = httpContextAccessor;
            _geocodingService = geocodingService;
        }

        // GET: Listings
        public async Task<IActionResult> Index(
            string? searchString,
<<<<<<< HEAD
            string? location,
            string? type,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? selectedTagIds,
            string? sortBy)
        {
            selectedTagIds ??= new List<int>();
=======
            List<int>? tagIds,
            decimal? minPrice,
            decimal? maxPrice,
            string? listingType,
            string? sortBy,
            int? maxDistanceKm,
            double? userLat,
            double? userLng
            )
        {
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.TagIds = tagIds ?? new List<int>();
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.ListingType = listingType ?? "";
            ViewBag.SortBy = sortBy ?? "newest";
            ViewBag.MaxDistanceKm = maxDistanceKm;
            ViewBag.UserLat = userLat;
            ViewBag.UserLng = userLng;

            IQueryable<Listing> query = _context.Listings
                .Include(l => l.Photos)
                    .ThenInclude(lp => lp.Upload)
                .Include(l => l.Reviews)
                .Include(l => l.Seller)
                    .ThenInclude(s => s.Listings)
                        .ThenInclude(sl => sl.Reviews)
                .Include(l => l.Tags)
                    .ThenInclude(lt => lt.Tag)
                .Where(l => l.IsArchived == false);
>>>>>>> origin/main

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

<<<<<<< HEAD
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
=======
            if (tagIds?.Any() == true)
                query = query.Where(l => l.Tags.Any(lt => tagIds.Contains(lt.TagId)));

            if (minPrice.HasValue)
                query = query.Where(l => l.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(l => l.Price <= maxPrice.Value);

            if (listingType == "sale")
                query = query.Where(l => l.Price.HasValue);
            else if (listingType == "trade")
                query = query.Where(l => !l.NotExchangeable);

            query = sortBy switch
            {
                "price_asc"  => query.OrderBy(l => l.Price),
                "price_desc" => query.OrderByDescending(l => l.Price),
                "views"      => query.OrderByDescending(l => l.ViewCount),
                _            => query.OrderByDescending(l => l.CreatedAt),
>>>>>>> origin/main
            };

            var listings = await query.ToListAsync();

<<<<<<< HEAD
            var results = listings.Select(l => new BrowseListingsViewModel
=======
            if (maxDistanceKm.HasValue && userLat.HasValue && userLng.HasValue)
            {
                listings = listings
                .Where(l =>
                    l.Seller?.Latitude.HasValue == true &&
                    l.Seller?.Longitude.HasValue == true &&
                    (l.Seller?.Latitude.Value != 0 || l.Seller?.Longitude.Value != 0) &&
                    _geocodingService.CalculateDistanceKm(
                        userLat.Value,
                        userLng.Value,
                        l.Seller.Latitude.Value,
                        l.Seller.Longitude.Value
                    ) <= maxDistanceKm.Value)
                .ToList();
            }

            var model = listings.Select(l => new BrowseListingsViewModel
>>>>>>> origin/main
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

<<<<<<< HEAD
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
=======
            if (sortBy == "rating")
                model = model.OrderByDescending(m => m.AverageRating).ToList();

            if (!model.Any())
                ViewBag.NoResultsMessage = "Nie znaleziono ofert spełniających kryteria.";
>>>>>>> origin/main

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

            foreach (var (field, message) in _fileService.ValidateImages(model.PhotoFiles, maxCount: 5))
                ModelState.AddModelError(field, message);

            if (!ModelState.IsValid)
            {
                _helper.PopulateAvailableTags(model);
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

<<<<<<< HEAD
            foreach (var opt in model.ShippingOptions.Where(o => !string.IsNullOrWhiteSpace(o.Name)))
            {
                listing.ShippingOptions.Add(new ListingShippingOption
                {
                    Name = opt.Name.Trim(),
                    Price = Math.Max(0, opt.Price)
                });
            }

            if (model.SelectedTagIds != null && model.SelectedTagIds.Any())
=======
            if (model.SelectedTagIds?.Any() == true)
>>>>>>> origin/main
            {
                foreach (var tagId in model.SelectedTagIds)
                {
                    var tag = _context.Tags.Find(tagId);
                    if (tag != null)
                        listing.Tags.Add(new ListingTag { TagId = tag.Id, Listing = listing });
                }
            }

            if (model.SelectedExchangeAcceptedTagIds?.Any() == true)
            {
                foreach (var tagId in model.SelectedExchangeAcceptedTagIds.Distinct())
                {
                    var tag = _context.Tags.Find(tagId);
                    if (tag != null)
                        listing.ExchangeAcceptedTags.Add(new ListingExchangeAcceptedTag { TagId = tag.Id, Listing = listing });
                }
            }

            if (model.PhotoFiles?.Count > 0)
            {
                bool first = true;
                foreach (var file in model.PhotoFiles)
                {
                    var upload = await _fileService.SaveFileAsync(file, currentUserId);
                    listing.Photos.Add(new ListingPhoto
                    {
                        Listing = listing,
                        Upload = upload,
                        IsFeatured = first
                    });
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
                listing.IsArchived = true;
                listing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.Id == id);
        }
    }
}
