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

namespace ProjektZespolowyGr3.Controllers.User
{
    public class ListingsController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _env;

        public ListingsController(MyDBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Listings
        public async Task<IActionResult> Index()
        {
            var myDBContext = _context.Listings.Include(l => l.Seller);
            return View(await myDBContext.ToListAsync());
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
            MakeSomeTags();
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
                PopulateAvailableTags(model);
                return View(model);
            }

            var userId = GetCurrentUserId();

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
                    listing.Tags.Add(new ListingTag { TagId = tagId });
                }
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            System.Diagnostics.Debug.WriteLine($"AAAAAAAAAAA: {model.PhotoFiles}");

            if (model.PhotoFiles != null && model.PhotoFiles.Count != 0)
            {
                System.Diagnostics.Debug.WriteLine($"Number of uploaded files: {model.PhotoFiles.Count}");
                foreach (var file in model.PhotoFiles.Take(5))
                {
                    if (file.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("PhotoFiles", "Each photo must be less than 5 MB.");
                        PopulateAvailableTags(model);
                        return View(model);
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("PhotoFiles", "Only .jpg, .jpeg, .png files are allowed.");
                        PopulateAvailableTags(model);
                        return View(model);
                    }
                }

                bool first = true; // pierwsze staje sie featured

                foreach (var file in model.PhotoFiles.Take(5))
                {

                    var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsPath))
                        Directory.CreateDirectory(uploadsPath);

                    var ext = Path.GetExtension(file.FileName);
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var relativeUrl = $"/uploads/{fileName}";

                    var upload = new Upload
                    {
                        FileName = file.FileName,
                        Extension = ext,
                        Url = relativeUrl,
                        SizeBytes = file.Length,
                        UploaderId = userId,
                        UploadedAt = DateTime.UtcNow
                    };
                    _context.Uploads.Add(upload);
                    await _context.SaveChangesAsync();

                    listing.Photos.Add(new ListingPhoto
                    {
                        UploadId = upload.Id,
                        IsFeatured = first,
                        Listing = listing
                    });

                    first = false;
                }
            }

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = listing.Id });
        }

        // ZMIENIC
        private int GetCurrentUserId()
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == 1);
            if (user == null)
            {
                var newUser = new Models.DbModels.User
                {
                    Id = 1,
                    Username = "testuser",
                    Email = "test@test.com",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(newUser);
                _context.SaveChanges();
                return newUser.Id;
            }
            else
            {
                return user.Id;
            }
        }

        // ZMIENIC
        private int MakeSomeTags()
        {
            if (!_context.Tags.Any())
            {
                var tags = new List<Tag>
                {
                    new Tag { Name = "Electronics" },
                    new Tag { Name = "Furniture" },
                    new Tag { Name = "Books" },
                    new Tag { Name = "Clothing" },
                    new Tag { Name = "Toys" }
                };
                _context.Tags.AddRange(tags);
                _context.SaveChanges();
            }
            return _context.Tags.Count();
        }

        private void PopulateAvailableTags(CreateListingViewModel model)
        {
            model.AvailableTags = _context.Tags
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem
                {
                    Text = t.Name,
                    Value = t.Id.ToString()
                })
                .ToList();
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
