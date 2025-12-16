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
        public MyDBContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ListingManageController(MyDBContext context, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IActionResult> Index(string? searchString = null, int pageSize = 25, int pageNumber = 1, string tagFilter = null)
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

            object? model = await GetFilteredListingsAsync(searchString, pageSize, pageNumber, tagFilter, userId, isAdmin);
            int totalClients = await GetListingsCountAsync(searchString, tagFilter, userId, isAdmin);

            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentPageSize = pageSize;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.CurrentTag = tagFilter;
            ViewBag.TotalPages = (int)Math.Ceiling(totalClients / (double)pageSize);

            return View(model);
        }

        private async Task<List<BrowseListingsViewModel>> GetFilteredListingsAsync(string? searchString, int pageSize, int pageNumber, string tagFilter, int userId, bool isAdmin)
        {
            var listings = _context.Listings
                        .Include(l => l.Photos).ThenInclude(lp => lp.Upload)
                        .Include(l => l.Reviews)
                        .Include(l => l.Seller)
                        .Include(l => l.Tags).ThenInclude(lt => lt.Tag)
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
                })
                .ToListAsync();
        }
        private async Task<int> GetListingsCountAsync(string? searchString, string tagFilter, int userId, bool isAdmin)
        {
            var listings = _context.Listings.AsQueryable();

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
                .FirstOrDefault(l => l.Id == id);

            if (listing == null)
                return NotFound();

            if (!isAdmin && listing.SellerId != userId)
                return Forbid();

            var vm = new EditListingViewModel
            {
                Title = listing.Title,
                Description = listing.Description,
                Type = listing.Type,
                Price = listing.Price,
                SelectedTagIds = listing.Tags.Select(t => t.TagId).ToList(),
                Photos = listing.Photos,
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
                .FirstOrDefault(l => l.Id == id);

            if (listing == null)
                return NotFound();

            if (!isAdmin && listing.SellerId != userId)
                return Forbid();

            if (!ModelState.IsValid)
            {
                vm.Photos = listing.Photos;
                ViewBag.ModelId = id;
                return View(vm);
            }

            listing.Title = vm.Title;
            listing.Description = vm.Description;
            listing.Price = vm.Price;
            listing.Type = vm.Type;

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

            if (vm.PhotosToDelete != null)
            {
                foreach (var photoId in vm.PhotosToDelete)
                {
                    var photo = listing.Photos.FirstOrDefault(x => x.Id == photoId);
                    if (photo != null)
                    {
                        var path = Path.Combine(_env.WebRootPath, photo.Upload.Url.TrimStart('/'));
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);

                        _context.Uploads.Remove(photo.Upload);
                        _context.ListingPhotos.Remove(photo);
                    }
                }
            }

            if (vm.PhotoFiles != null && vm.PhotoFiles.Count > 0)
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                foreach (var file in vm.PhotoFiles)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var path = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                        await file.CopyToAsync(stream);

                    var upload = new Upload
                    {
                        FileName = file.FileName,
                        Extension = ext,
                        Url = "/uploads/" + fileName,
                        SizeBytes = file.Length,
                        UploadedAt = DateTime.UtcNow,
                        UploaderId = listing.SellerId
                    };

                    var newPhoto = new ListingPhoto
                    {
                        ListingId = listing.Id,
                        Upload = upload,
                        IsFeatured = false
                    };

                    listing.Photos.Add(newPhoto);
                }
            }

            listing.UpdatedAt = DateTime.UtcNow;

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

            _context.ListingPhotos.Where(lp => lp.ListingId == id)
                .Include(lp => lp.Upload)
                .ToList()
                .ForEach(lp =>
                {
                    var path = Path.Combine(_env.WebRootPath, lp.Upload.Url.TrimStart('/'));
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);

                    _context.Uploads.Remove(lp.Upload);
                });
            _context.Listings.Remove(listing);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
