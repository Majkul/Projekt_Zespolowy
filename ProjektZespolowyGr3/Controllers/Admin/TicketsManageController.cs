using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class TicketsManageController : Controller
    {
        private readonly MyDBContext _context;

        public TicketsManageController(MyDBContext context)
        {
            _context = context;
        }

        // GET: Admin/TicketsManage
        public async Task<IActionResult> Index(string? searchString = null, TicketStatus? statusFilter = null, TicketCategory? categoryFilter = null, int? assigneeFilter = null, int pageSize = 25, int pageNumber = 1)
        {
            var query = _context.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.ReportedListing)
                .Include(t => t.ReportedUser)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t =>
                    t.Subject.Contains(searchString) ||
                    t.Description.Contains(searchString) ||
                    t.User.Username.Contains(searchString));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.Status == statusFilter.Value);
            }

            if (categoryFilter.HasValue)
            {
                query = query.Where(t => t.Category == categoryFilter.Value);
            }

            if (assigneeFilter.HasValue)
            {
                query = query.Where(t => t.AssigneeId == assigneeFilter.Value);
            }

            var totalTickets = await query.CountAsync();
            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentFilter = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CategoryFilter = categoryFilter;
            ViewBag.AssigneeFilter = assigneeFilter;
            ViewBag.CurrentPageSize = pageSize;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalTickets / (double)pageSize);

            // Lista adminów do przypisania
            var admins = await _context.Users
                .Where(u => u.IsAdmin)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Username
                })
                .ToListAsync();

            ViewBag.Admins = admins;
            ViewBag.Statuses = Enum.GetValues(typeof(TicketStatus)).Cast<TicketStatus>().Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.ToString()
            }).ToList();

            ViewBag.Categories = Enum.GetValues(typeof(TicketCategory)).Cast<TicketCategory>().Select(c => new SelectListItem
            {
                Value = c.ToString(),
                Text = c.ToString()
            }).ToList();

            return View(tickets);
        }

        // GET: Admin/TicketsManage/Details/5
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
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var admins = await _context.Users
                .Where(u => u.IsAdmin)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Username,
                    Selected = u.Id == ticket.AssigneeId
                })
                .ToListAsync();

            ViewBag.Admins = admins;
            ViewBag.Statuses = Enum.GetValues(typeof(TicketStatus)).Cast<TicketStatus>().Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.ToString(),
                Selected = s == ticket.Status
            }).ToList();

            return View(ticket);
        }

        // POST: Admin/TicketsManage/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, TicketStatus status)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = status;
            ticket.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["AlertSuccess"] = "Status ticketu został zaktualizowany.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/TicketsManage/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, int? assigneeId)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            if (assigneeId.HasValue)
            {
                var assignee = await _context.Users.FindAsync(assigneeId.Value);
                if (assignee == null || !assignee.IsAdmin)
                {
                    TempData["AlertError"] = "Nieprawidłowy administrator.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                ticket.AssigneeId = assigneeId.Value;
            }
            else
            {
                ticket.AssigneeId = null;
            }

            ticket.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["AlertSuccess"] = "Ticket został przypisany.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

