using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;

namespace ProjektZespolowyGr3.Controllers.User
{
    public class TicketsController : Controller
    {
        private readonly MyDBContext _context;
        private readonly HelperService _helper;
        private readonly IWebHostEnvironment _env;

        public TicketsController(MyDBContext context, HelperService helper, IWebHostEnvironment env)
        {
            _context = context;
            _helper = helper;
            _env = env;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            var myDBContext = _context.Tickets.Include(t => t.Assignee).Include(t => t.ReportedListing).Include(t => t.ReportedUser).Include(t => t.User);
            return View(await myDBContext.ToListAsync());
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.ReportedListing)
                .Include(t => t.ReportedUser)
                .Include(t => t.User)
                .Include(t => t.Attachments)
                    .ThenInclude(a => a.Upload)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        [HttpGet]
        public IActionResult ReportUser(int userId)
        {
            var vm = new CreateTicketViewModel
            {
                Category = TicketCategory.User_Report,
                ReportedUserId = userId,
                ReportedUserName = _context.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefault()
            };
            return View("Create", vm);
        }

        [HttpGet]
        public IActionResult ReportListing(int listingId)
        {
            var vm = new CreateTicketViewModel
            {
                Category = TicketCategory.Listing_Report,
                ReportedListingId = listingId,
                ReportedListingTitle = _context.Listings.Where(l => l.Id == listingId).Select(l => l.Title).FirstOrDefault()
            };
            return View("Create", vm);
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {
            var vm = new CreateTicketViewModel
            {
                Category = TicketCategory.Other_Issue
            };
            return View(vm);
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTicketViewModel model)
        {
            if (model.Category == TicketCategory.User_Report && model.ReportedUserId == null)
            {
                ModelState.AddModelError(string.Empty, "No user specified.");
            }

            if (model.Category == TicketCategory.Listing_Report && model.ReportedListingId == null)
            {
                ModelState.AddModelError(string.Empty, "No listing specified.");
            }

            if (!ModelState.IsValid) {
                return View(model);
            }

            if (model.Attachments.Count > 10)
            {
                ModelState.AddModelError("Attachments", "You can upload a maximum of 10 attachments.");
                return View(model);
            }

            // TODO: zmienic na fatkycznie dzialajace
            var userId = _helper.GetCurrentUserId();

            var ticket = new Ticket
            {
                UserId = userId,
                Category = model.Category,
                Status = TicketStatus.Open,
                Subject = model.Subject,
                Description = model.Description,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                ReportedUserId = model.ReportedUserId,
                ReportedListingId = model.ReportedListingId
            };

            // moze jakies dozwolone typy plikow ale idk
            //var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            if (model.Attachments != null && model.Attachments.Count > 0)
            {
                foreach (var file in model.Attachments)
                {
                    if (file.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("Attachments", "Each attachment must be less than 10 MB.");
                        return View(model);
                    }

                    //var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    //if (!allowedExtensions.Contains(ext))
                    //{
                    //    ModelState.AddModelError("Attachments", "Only .jpg, .jpeg, .png files are allowed.");
                    //    return View(model);
                    //}

                    //if (!file.ContentType.StartsWith("image/"))
                    //{
                    //    ModelState.AddModelError("PhotoFiles", "Invalid file type.");
                    //    return View(model);
                    //}
                }

                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // to pewnie bedzie mozna do jakiegos serwisu przerzucic pozniej bo sie powtarza kilkukrotnie
                foreach (var file in model.Attachments)
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

                    var ticketUpload = new TicketAttachment
                    {
                        Ticket = ticket,
                        Upload = upload
                    };

                    _context.TicketAttachments.Add(ticketUpload);
                }
            }

            _context.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = ticket.Id });
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            ViewData["AssigneeId"] = new SelectList(_context.Users, "Id", "Id", ticket.AssigneeId);
            ViewData["ReportedListingId"] = new SelectList(_context.Listings, "Id", "Id", ticket.ReportedListingId);
            ViewData["ReportedUserId"] = new SelectList(_context.Users, "Id", "Id", ticket.ReportedUserId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", ticket.UserId);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Category,Status,Subject,Description,AssigneeId,CreatedAt,LastActivity,ReportedUserId,ReportedListingId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
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
            ViewData["AssigneeId"] = new SelectList(_context.Users, "Id", "Id", ticket.AssigneeId);
            ViewData["ReportedListingId"] = new SelectList(_context.Listings, "Id", "Id", ticket.ReportedListingId);
            ViewData["ReportedUserId"] = new SelectList(_context.Users, "Id", "Id", ticket.ReportedUserId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", ticket.UserId);
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.ReportedListing)
                .Include(t => t.ReportedUser)
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }

        public string GetTicketCategoryDisplayName(TicketCategory category)
        {
            var type = typeof(TicketCategory);
            var memInfo = type.GetMember(category.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
            return (attributes.Length > 0) ? ((DisplayAttribute)attributes[0]).Name : category.ToString();
        }
    }
}
