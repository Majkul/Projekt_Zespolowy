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
using ProjektZespolowyGr3.Helpers;
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

        public ListingsController(MyDBContext context, IFileService fileService, AuthService auth, HelperService helper, IHttpContextAccessor httpContextAccessor, IGeocodingService geocodingService)
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
            List<int>? tagIds,
            decimal? minPrice,
            decimal? maxPrice,
            string? listingType,
            string? sortBy,
            int? maxDistanceKm,
            double? userLat,
            double? userLng,
            int page = 1,
            int pageSize = 24
            )
        {
            pageSize = ClampPageSize(pageSize);
            page = Math.Max(1, page);

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
            ViewBag.PageSize = pageSize;

            IQueryable<Listing> query = _context.Listings
                .Include(l => l.Photos)
                    .ThenInclude(lp => lp.Upload)
                .Include(l => l.Reviews)
                .Include(l => l.Seller)
                    .ThenInclude(s => s.Listings)
                        .ThenInclude(sl => sl.Reviews)
                .Include(l => l.Tags)
                    .ThenInclude(lt => lt.Tag)
                .Where(l => l.IsArchived == false && l.IsPrivate == false && l.StockQuantity > 0);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                query = query.Where(l =>
                    EF.Functions.ILike(l.Title, $"%{term}%") ||
                    (l.Description != null && EF.Functions.ILike(l.Description, $"%{term}%")));
            }

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

            var requiresInMemoryPagination = sortBy == "rating" || (maxDistanceKm.HasValue && userLat.HasValue && userLng.HasValue);

            if (sortBy != "rating")
            {
                query = sortBy switch
                {
                    "price_asc"  => query.OrderBy(l => l.Price.HasValue ? 0 : 1).ThenBy(l => l.Price),
                    "price_desc" => query.OrderBy(l => l.Price.HasValue ? 0 : 1).ThenByDescending(l => l.Price),
                    "views"      => query.OrderByDescending(l => l.ViewCount),
                    _            => query.OrderByDescending(l => l.CreatedAt),
                };
            }

            List<BrowseListingsViewModel> model;
            int totalCount;

            if (requiresInMemoryPagination)
            {
                var listings = await query.ToListAsync();

                if (maxDistanceKm.HasValue && userLat.HasValue && userLng.HasValue)
                {
                    listings = listings
                    .Where(l =>
                        l.Seller is { Latitude: double sellerLat, Longitude: double sellerLng } &&
                        (sellerLat != 0 || sellerLng != 0) &&
                        _geocodingService.CalculateDistanceKm(
                            userLng.Value,
                            userLat.Value,
                            sellerLng,
                            sellerLat
                        ) <= maxDistanceKm.Value)
                    .ToList();
                }

                model = listings.Select(ToBrowseListingsViewModel).ToList();

                if (sortBy == "rating")
                    model = model.OrderByDescending(m => m.AverageRating).ToList();

                totalCount = model.Count;
                var totalPages = GetTotalPages(totalCount, pageSize);
                page = Math.Min(page, totalPages);
                model = model
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                totalCount = await query.CountAsync();
                var totalPages = GetTotalPages(totalCount, pageSize);
                page = Math.Min(page, totalPages);

                var listings = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                model = listings.Select(ToBrowseListingsViewModel).ToList();
            }

            var finalTotalPages = GetTotalPages(totalCount, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = finalTotalPages;
            ViewBag.TotalCount = totalCount;

            if (!model.Any())
                ViewBag.NoResultsMessage = "Nie znaleziono ofert spełniających kryteria.";

            ViewData["HideNavSearch"] = true;

            return View(model);
        }

        private static int ClampPageSize(int pageSize)
        {
            return pageSize switch
            {
                < 1 => 24,
                > 96 => 96,
                _ => pageSize
            };
        }

        private static int GetTotalPages(int totalCount, int pageSize)
        {
            return Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        private static BrowseListingsViewModel ToBrowseListingsViewModel(Listing l)
        {
            return new BrowseListingsViewModel
            {
                Listing = l,
                ListingId = l.Id,
                Seller = l.Seller,
                SellerId = l.SellerId,
                PhotoUrl = (l.Photos.FirstOrDefault(lp => lp.IsFeatured) ?? l.Photos.FirstOrDefault())?.Upload.Url,
                AverageRating = l.Seller.Listings.SelectMany(sl => sl.Reviews).Any()
                    ? l.Seller.Listings.SelectMany(sl => sl.Reviews).Average(r => r.Rating)
                    : 0,
                ReviewCount = l.Seller.Listings.SelectMany(sl => sl.Reviews).Count(),
            };
        }

        // GET: Listings/slug-5
        [Route("Listings/{slug}-{id:int}")]
        public async Task<IActionResult> Details(string slug, int id)
        {
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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing == null)
            {
                var deletedListing = await _context.Listings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted);

                if (deletedListing != null)
                {
                    return View("Deleted");
                }

                return ListingNotFound();
            }

            if (listing.IsPrivate)
            {
                var viewerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(viewerIdClaim, out var viewerId) || viewerId != listing.SellerId)
                {
                    // jesli jest czlonkiem lancuszka wymian to moze i tak
                    bool isAllowedByTrade = await _context.TradeProposalItems
                        .AnyAsync(item => item.ListingId == listing.Id &&
                                         (item.TradeProposal.InitiatorUserId == viewerId || item.TradeProposal.ReceiverUserId == viewerId));

                    if (!isAllowedByTrade)
                        return ListingNotFound();
                }
            }

            listing.ViewCount++;
            await _context.SaveChangesAsync();

            ViewBag.CanReview = false;
            ViewBag.HasReviewed = false;
            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId != listing.SellerId)
            {
                var hasReviewed = await _context.Reviews
                    .AnyAsync(r => r.ListingId == listing.Id && r.ReviewerId == currentUserId);
                var boughtListing = await _context.Orders
                    .AnyAsync(o => o.ListingId == listing.Id && o.BuyerId == currentUserId && o.Status == OrderStatus.Paid);
                ViewBag.HasReviewed = hasReviewed;
                ViewBag.CanReview = boughtListing && !hasReviewed;
            }

            return View(listing);
        }

        // GET: Listings/Details/5
        [Route("Listings/Details/{id:int}")]
        public async Task<IActionResult> DetailsById(int id)
        {
            var listing = await _context.Listings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return ListingNotFound();
            }

            return RedirectToActionPermanent(nameof(Details), new
            {
                slug = SlugHelper.GenerateSlug(listing.Title),
                id = listing.Id
            });
        }

        private IActionResult ListingNotFound()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }

        // GET: Listings/Create
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int? forTradeListingId)
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

            if (forTradeListingId.HasValue)
            {
                var tradeListing = await _context.Listings
                    .Include(l => l.ExchangeAcceptedTags).ThenInclude(e => e.Tag)
                    .FirstOrDefaultAsync(l => l.Id == forTradeListingId.Value);

                if (tradeListing != null)
                {
                    ViewBag.ForTradeListingId = tradeListing.Id;
                    ViewBag.ForTradeListingTitle = tradeListing.Title;
                    ViewBag.ForTradeAcceptedTagIds = tradeListing.ExchangeAcceptedTags.Select(e => e.TagId).ToList();
                    ViewBag.ForTradeAcceptedTagNames = tradeListing.ExchangeAcceptedTags.Select(e => e.Tag.Name).ToList();

                    model.SelectedTagIds = tradeListing.ExchangeAcceptedTags.Select(e => e.TagId).ToList();
                    model.NotExchangeable = false;
                    model.IsPrivate = true;
                }
            }

            return View(model);
        }

        // POST: Listings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateListingViewModel model, int? forTradeListingId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId) || currentUserId <= 0)
            {
                return Challenge();
            }

            List<int> requiredTagIds = new();
            if (forTradeListingId.HasValue)
            {
                var tradeListing = await _context.Listings
                    .Include(l => l.ExchangeAcceptedTags)
                    .FirstOrDefaultAsync(l => l.Id == forTradeListingId.Value);

                if (tradeListing != null)
                {
                    model.IsPrivate = true;
                    model.NotExchangeable = false;
                    model.IsFeatured = false;
                    requiredTagIds = tradeListing.ExchangeAcceptedTags.Select(e => e.TagId).ToList();

                    if (requiredTagIds.Count > 0 && (model.SelectedTagIds == null || !model.SelectedTagIds.Any(id => requiredTagIds.Contains(id))))
                    {
                        ModelState.AddModelError("SelectedTagIds", "Co najmniej jeden tag musi pasować do akceptowanych tagów ogłoszenia, do którego tworzysz ofertę.");
                    }
                }
            }

            if (model.StockQuantity < 1)
                ModelState.AddModelError(nameof(CreateListingViewModel.StockQuantity), "Liczba sztuk musi być co najmniej 1.");

            foreach (var (field, message) in _fileService.ValidateImages(model.PhotoFiles, maxCount: 5))
                ModelState.AddModelError(field, message);

            AddEmptyPhotoFileErrors(model.PhotoFiles, nameof(CreateListingViewModel.PhotoFiles));

            if (!ModelState.IsValid)
            {
                _helper.PopulateAvailableTags(model);
                if (forTradeListingId.HasValue)
                {
                    ViewBag.ForTradeListingId = forTradeListingId.Value;
                    if (requiredTagIds.Count > 0)
                        ViewBag.ForTradeAcceptedTagIds = requiredTagIds;
                }
                return View(model);
            }

            var notExchangeable = forTradeListingId.HasValue ? false : model.NotExchangeable;
            var listing = new Listing
            {
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                SellerId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                NotExchangeable = notExchangeable,
                IsPrivate = forTradeListingId.HasValue ? true : model.IsPrivate,
                IsFeatured = forTradeListingId.HasValue ? false : model.IsFeatured,
                MinExchangeValue = notExchangeable ? null : model.MinExchangeValue,
                ExchangeDescription = notExchangeable || string.IsNullOrWhiteSpace(model.ExchangeDescription)
                    ? null
                    : model.ExchangeDescription.Trim()
            };
            ListingStockHelper.SyncSoldFlag(listing);

            if (model.SelectedTagIds?.Any() == true)
            {
                foreach (var tagId in model.SelectedTagIds)
                {
                    var tag = _context.Tags.Find(tagId);
                    if (tag != null)
                        listing.Tags.Add(new ListingTag { TagId = tag.Id, Listing = listing });
                }
            }

            if (!notExchangeable && model.SelectedExchangeAcceptedTagIds?.Any() == true)
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
                    if (file == null || file.Length == 0)
                        continue;

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

            if (forTradeListingId.HasValue)
                return RedirectToAction("Compose", "TradeProposals", new { listingId = forTradeListingId.Value });

            return RedirectToAction("Details", new { slug = SlugHelper.GenerateSlug(listing.Title), id = listing.Id });
        }

        // GET: Listings/
        // /5
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
                listing.IsDeleted = true;
                listing.DeletedAt = DateTime.UtcNow;
                listing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.Id == id);
        }

        private void AddEmptyPhotoFileErrors(IList<IFormFile>? files, string field)
        {
            if (files == null)
                return;

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    ModelState.AddModelError(field, "Wybrane zdjęcie jest niedostępne. Wybierz plik ponownie.");
                }
            }
        }
    }
}
