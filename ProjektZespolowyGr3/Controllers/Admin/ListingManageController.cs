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
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace DomPogrzebowyProjekt.Controllers.Admin
{
    [Authorize(Roles = "Admin,Client")]
    public class ListingManageController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IFileService _fileService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ListingManageController(MyDBContext context, IFileService fileService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _fileService = fileService;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IActionResult> Index(string? searchString = null, int pageSize = 25, int pageNumber = 1, string? tagFilter = null, bool showArchived = false)
        {
            int userId = 0;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    int.TryParse(userIdClaim, out userId);
                }
            }

            object? model = await GetFilteredListingsAsync(searchString, pageSize, tagFilter, userId, isAdmin, showArchived);

            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentPageSize = pageSize;
            ViewBag.CurrentTag = tagFilter;
            ViewBag.ShowArchived = showArchived;
            ViewBag.AllTags = await _context.Tags
                .OrderBy(t => t.Name)
                .Select(t => t.Name)
                .ToListAsync();

            return View(model);
        }

        private IQueryable<Listing> BuildListingManageQuery(string? searchString, string? tagFilter, int userId, bool isAdmin, bool showArchived)
        {
            var listings = _context.Listings
                .IgnoreQueryFilters()
                .Include(l => l.Photos).ThenInclude(lp => lp.Upload)
                .Include(l => l.Reviews)
                .Include(l => l.Seller)
                .Include(l => l.Tags).ThenInclude(lt => lt.Tag)
                .AsQueryable();

            listings = showArchived
                ? listings.Where(l => l.IsArchived || l.IsDeleted || l.IsSold || l.StockQuantity <= 0)
                : listings.Where(l => !l.IsArchived && !l.IsDeleted && !l.IsSold && l.StockQuantity > 0);

            if (!isAdmin)
            {
                listings = listings.Where(l => l.SellerId == userId);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                if (UsesNpgsqlProvider())
                {
                    listings = listings.Where(l =>
                        l.Id.ToString().Contains(term) ||
                        EF.Functions.ILike(l.Title, $"%{term}%") ||
                        (l.Seller != null && EF.Functions.ILike(l.Seller.Username, $"%{term}%")));
                }
                else
                {
                    var loweredTerm = term.ToLower();
                    listings = listings.Where(l =>
                        l.Id.ToString().Contains(term) ||
                        l.Title.ToLower().Contains(loweredTerm) ||
                        (l.Seller != null && l.Seller.Username.ToLower().Contains(loweredTerm)));
                }
            }

            if (!string.IsNullOrWhiteSpace(tagFilter))
            {
                listings = listings.Where(l => l.Tags
                    .Any(lt => lt.Tag.Name == tagFilter));
            }

            return listings;
        }

        private bool UsesNpgsqlProvider()
        {
            return _context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        }

        private async Task<List<BrowseListingsViewModel>> GetFilteredListingsAsync(string? searchString, int pageSize, string? tagFilter, int userId, bool isAdmin, bool showArchived = false)
        {
            // Stronicowanie odbywa się po stronie przeglądarki (admin-pagination.js),
            // dlatego z bazy pobieramy wszystkie pasujące rekordy.
            return await BuildListingManageQuery(searchString, tagFilter, userId, isAdmin, showArchived)
                .OrderBy(l => l.Id)
                .Select(l => new BrowseListingsViewModel
                {
                    Listing = l,
                ListingId = l.Id,
                Seller = l.Seller,
                SellerId = l.SellerId,
                //PhotoUrl = l.Photos.FirstOrDefault(lp => lp.IsFeatured)?.Upload.Url,
                AverageRating = l.Seller.Listings
                        .SelectMany(sl => sl.Reviews)
                        .Any()
                        ? l.Seller.Listings.SelectMany(sl => sl.Reviews).Average(r => r.Rating)
                        : 0,
                ReviewCount = l.Seller.Listings.SelectMany(sl => sl.Reviews).Count(),
                IsArchived = l.IsArchived
                })
                .ToListAsync();
        }

        [HttpGet]
        public IActionResult EditListing(int id, string? returnUrl = null)
        {
            int userId = 0;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    int.TryParse(userIdClaim, out userId);
                }
            }

            var listing = _context.Listings
                .Include(l => l.Photos).ThenInclude(p => p.Upload)
                .Include(l => l.Tags)
                .Include(l => l.ExchangeAcceptedTags)
                .Include(l => l.ShippingOptions)
                .FirstOrDefault(l => l.Id == id);

            if (listing == null)
                return NotFound();

            if (!isAdmin && listing.SellerId != userId)
                return Forbid();

            var vm = new EditListingViewModel
            {
                Title = listing.Title,
                Description = listing.Description,
                Location = listing.Location,
                Type = listing.Type,
                Price = listing.Price,
                SelectedTagIds = listing.Tags.Select(t => t.TagId).ToList(),
                Photos = listing.Photos,
                NotExchangeable = listing.NotExchangeable,
                IsPrivate = listing.IsPrivate,
                MinExchangeValue = listing.MinExchangeValue,
                ExchangeDescription = listing.ExchangeDescription,
                StockQuantity = listing.StockQuantity,
                IsFeatured = listing.IsFeatured,
                SelectedExchangeAcceptedTagIds = listing.ExchangeAcceptedTags.Select(e => e.TagId).ToList(),
                ShippingOptions = listing.ShippingOptions.Select(o => new ShippingOptionInput
                {
                    Name = o.Name,
                    Price = o.Price
                }).ToList(),
                AvailableTags = _context.Tags.Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name
                })
            };

            ViewBag.ModelId = id;
            ViewBag.ReturnUrl = LocalReturnUrlOrNull(returnUrl);

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditListing(int id, EditListingViewModel vm, string? returnUrl = null)
        {
            int userId = 0;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    int.TryParse(userIdClaim, out userId);
                }
            }

            var listing = _context.Listings
                .Include(l => l.Photos).ThenInclude(p => p.Upload)
                .Include(l => l.Tags)
                .Include(l => l.ExchangeAcceptedTags)
                .Include(l => l.ShippingOptions)
                .FirstOrDefault(l => l.Id == id);

            if (listing == null)
                return NotFound();

            if (!isAdmin && listing.SellerId != userId)
                return Forbid();

            foreach (var (field, message) in _fileService.ValidateImages(vm.PhotoFiles))
                ModelState.AddModelError(field, message);

            AddEmptyPhotoFileErrors(vm.PhotoFiles, nameof(EditListingViewModel.PhotoFiles));

            if (!ModelState.IsValid)
            {
                PopulateEditListingForm(vm, listing, id, returnUrl);
                ViewBag.ModelId = id;
                return View(vm);
            }

            listing.Title = vm.Title;
            listing.Description = vm.Description;
            listing.Location = string.IsNullOrWhiteSpace(vm.Location) ? null : vm.Location.Trim();
            listing.Price = vm.Price;
            listing.NotExchangeable = vm.NotExchangeable;
            listing.IsPrivate = vm.IsPrivate;
            listing.MinExchangeValue = vm.NotExchangeable ? null : vm.MinExchangeValue;
            listing.ExchangeDescription = vm.NotExchangeable || string.IsNullOrWhiteSpace(vm.ExchangeDescription)
                ? null
                : vm.ExchangeDescription.Trim();
            listing.StockQuantity = vm.StockQuantity;
            listing.IsFeatured = vm.IsFeatured;
            ListingStockHelper.SyncSoldFlag(listing);

            listing.Tags.Clear();
            if (vm.SelectedTagIds != null)
            {
                foreach (var tagId in vm.SelectedTagIds)
                {
                    listing.Tags.Add(new ListingTag
                    {
                        TagId = tagId,
                        ListingId = listing.Id
                    });
                }
            }

            listing.ExchangeAcceptedTags.Clear();
            if (!vm.NotExchangeable && vm.SelectedExchangeAcceptedTagIds != null)
            {
                foreach (var tagId in vm.SelectedExchangeAcceptedTagIds.Distinct())
                {
                    listing.ExchangeAcceptedTags.Add(new ListingExchangeAcceptedTag
                    {
                        TagId = tagId,
                        ListingId = listing.Id
                    });
                }
            }

            _context.ListingShippingOptions.RemoveRange(
                _context.ListingShippingOptions.Where(o => o.ListingId == listing.Id));
            if (vm.ShippingOptions != null)
            {
                foreach (var opt in vm.ShippingOptions.Where(o => !string.IsNullOrWhiteSpace(o.Name)))
                {
                    listing.ShippingOptions.Add(new ListingShippingOption
                    {
                        Name = opt.Name.Trim(),
                        Price = Math.Max(0, opt.Price),
                        ListingId = listing.Id
                    });
                }
            }

            if (vm.PhotosToDelete != null)
            {
                foreach (var photoId in vm.PhotosToDelete)
                {
                    var photo = listing.Photos.FirstOrDefault(x => x.Id == photoId);
                    if (photo != null)
                    {
                        _fileService.DeleteFile(photo.Upload);
                        _context.ListingPhotos.Remove(photo);
                    }
                }
            }

            if (vm.PhotoFiles?.Count > 0)
            {
                foreach (var file in vm.PhotoFiles)
                {
                    if (file == null || file.Length == 0)
                        continue;

                    var upload = await _fileService.SaveFileAsync(file, listing.SellerId);
                    listing.Photos.Add(new ListingPhoto
                    {
                        ListingId = listing.Id,
                        Upload = upload,
                        IsFeatured = false
                    });
                }
            }

            listing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectAfterListingMutation(returnUrl);
        }


        [HttpPost]
        public async Task<IActionResult> ToggleFeature(int id)
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            listing.IsFeatured = !listing.IsFeatured;
            listing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreListing(int id)
        {
            int userId = 0;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdClaim))
                    int.TryParse(userIdClaim, out userId);
            }

            var listing = _context.Listings
                .IgnoreQueryFilters()
                .FirstOrDefault(l => l.Id == id);
            if (listing == null) return NotFound();
            if (!isAdmin && listing.SellerId != userId) return Forbid();

            if (!CanRestoreListing(listing))
                return BadRequest();

            listing.IsArchived = false;
            listing.UpdatedAt = DateTime.UtcNow;

            var relatedTickets = await _context.Tickets
                .Where(t => t.ReportedListingId == id)
                .ToListAsync();
            foreach (var ticket in relatedTickets)
                ticket.IsArchived = listing.IsArchived;

            var relatedMessages = await _context.Messages
                .Where(m => m.ListingId == id)
                .ToListAsync();
            foreach (var message in relatedMessages)
                message.IsArchived = listing.IsArchived;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        private static bool CanRestoreListing(Listing listing)
        {
            return listing.IsArchived && !listing.IsDeleted && !listing.IsSold && listing.StockQuantity > 0;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteListing(int id, string? returnUrl = null)
        {
            int userId = 0;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    int.TryParse(userIdClaim, out userId);
                }
            }

            var listing = _context.Listings.Find(id);
            if (listing == null) return NotFound();

            if (!isAdmin && listing.SellerId != userId)
                return Forbid();

            _context.ListingPhotos
                .Where(lp => lp.ListingId == id)
                .Include(lp => lp.Upload)
                .ToList()
                .ForEach(lp => _fileService.DeleteFile(lp.Upload));
            listing.IsArchived = true;
            listing.IsDeleted = true;
            listing.DeletedAt = DateTime.UtcNow;
            listing.UpdatedAt = DateTime.UtcNow;

            _context.Tickets
                .Where(t => t.ReportedListingId == id)
                .ToList()
                .ForEach(t => _context.Tickets.Remove(t));

            _context.Messages
                .IgnoreQueryFilters()
                .Where(m => m.ListingId == id)
                .ToList()
                .ForEach(m =>
                {
                    m.IsDeleted = true;
                    m.DeletedAt = DateTime.UtcNow;
                });

            _context.SaveChanges();

            return RedirectAfterListingMutation(returnUrl);
        }

        private void PopulateEditListingForm(EditListingViewModel vm, Listing listing, int id, string? returnUrl)
        {
            vm.Photos = listing.Photos;
            vm.AvailableTags = _context.Tags
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name
                })
                .ToList();
            ViewBag.ModelId = id;
            ViewBag.ReturnUrl = LocalReturnUrlOrNull(returnUrl);
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

        private IActionResult RedirectAfterListingMutation(string? returnUrl)
        {
            return IsLocalReturnUrl(returnUrl)
                ? LocalRedirect(returnUrl!)
                : RedirectToAction("Index");
        }

        private static string? LocalReturnUrlOrNull(string? returnUrl)
        {
            return IsLocalReturnUrl(returnUrl) ? returnUrl : null;
        }

        private static bool IsLocalReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
                return false;

            if (returnUrl[0] == '/')
                return returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\');

            return returnUrl.Length > 1 && returnUrl[0] == '~' && returnUrl[1] == '/';
        }
    }
}
