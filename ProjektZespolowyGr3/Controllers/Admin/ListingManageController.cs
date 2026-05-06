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

            object? model = await GetFilteredListingsAsync(searchString, pageSize, pageNumber, tagFilter, userId, isAdmin, showArchived);
            int totalClients = await GetListingsCountAsync(searchString, tagFilter, userId, isAdmin, showArchived);

            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentPageSize = pageSize;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.CurrentTag = tagFilter;
            ViewBag.ShowArchived = showArchived;
            ViewBag.TotalPages = (int)Math.Ceiling(totalClients / (double)pageSize);

            return View(model);
        }

        private async Task<List<BrowseListingsViewModel>> GetFilteredListingsAsync(string? searchString, int pageSize, int pageNumber, string? tagFilter, int userId, bool isAdmin, bool showArchived = false)
        {
            var listings = _context.Listings
                        .Include(l => l.Photos).ThenInclude(lp => lp.Upload)
                        .Include(l => l.Reviews)
                        .Include(l => l.Seller)
                        .Include(l => l.Tags).ThenInclude(lt => lt.Tag)
                        .Where(l => l.IsArchived == showArchived)
                        .AsQueryable();

            if (!isAdmin)
            {
                listings = listings.Where(l => l.SellerId == userId);
            }
            var tags = _context.ListingTags.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                listings = listings.Where(c =>
                    c.Id.ToString().Contains(searchString) ||
                    c.Title.Contains(searchString)); //Dodać ewnetualnie szuaknie po nazwach sprzedawców
            }

            if (!string.IsNullOrEmpty(tagFilter))
            {
                listings = listings.Where(l => l.Tags
                    .Any(lt => lt.Tag.Name == tagFilter));
            }

            return await listings
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
        private async Task<int> GetListingsCountAsync(string? searchString, string? tagFilter, int userId, bool isAdmin, bool showArchived = false)
        {
            var listings = _context.Listings
                .Where(l => l.IsArchived == showArchived)
                .AsQueryable();

            if (!isAdmin)
            {
                listings = listings.Where(l => l.SellerId == userId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                listings = listings.Where(c =>
                    c.Id.ToString().Contains(searchString) ||
                    c.Title.Contains(searchString)); //Dodać ewnetualnie szuaknie po nazwach sprzedawców
            }

            if (!string.IsNullOrEmpty(tagFilter))
            {
                listings = listings.Where(l => l.Tags
                    .Any(lt => lt.Tag.Name == tagFilter));
            }
            return await listings.CountAsync();
        }

        [HttpGet]
        public IActionResult EditListing(int id)
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

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> EditListing(int id, EditListingViewModel vm)
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

            if (!ModelState.IsValid)
            {
                vm.Photos = listing.Photos;
                ViewBag.ModelId = id;
                return View(vm);
            }

            listing.Title = vm.Title;
            listing.Description = vm.Description;
            listing.Location = string.IsNullOrWhiteSpace(vm.Location) ? null : vm.Location.Trim();
            listing.Price = vm.Price;
            listing.NotExchangeable = vm.NotExchangeable;
            listing.MinExchangeValue = vm.MinExchangeValue;
            listing.ExchangeDescription = string.IsNullOrWhiteSpace(vm.ExchangeDescription) ? null : vm.ExchangeDescription.Trim();
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
            if (vm.SelectedExchangeAcceptedTagIds != null)
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

            return RedirectToAction("Index");
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
        public async Task<IActionResult> ArchiveListing(int id)
        {
            int userId = 0;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdClaim))
                    int.TryParse(userIdClaim, out userId);
            }

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();
            if (!isAdmin && listing.SellerId != userId) return Forbid();

            listing.IsArchived = !listing.IsArchived;
            listing.ArchivedAt = listing.IsArchived ? DateTime.UtcNow : null;
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

        [HttpPost]
        public IActionResult DeleteListing(int id)
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

            _context.Tickets
                .Where(t => t.ReportedListingId == id)
                .ToList()
                .ForEach(t => _context.Tickets.Remove(t));

            _context.Messages
                .Where(m => m.ListingId == id)
                .ToList()
                .ForEach(m => m.IsArchived = true);

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
