using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly MyDBContext _context;

        public MessagesController(MyDBContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idString) || !int.TryParse(idString, out var id))
            {
                throw new Exception("Brak zalogowanego użytkownika");
            }
            return id;
        }

        // GET: /Messages
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var messages = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Listing)
                .Include(m => m.Ticket)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => new
                {
                    OtherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
                    m.ListingId,
                    m.TicketId
                })
                .Select(g => g.OrderByDescending(m => m.SentAt).First())
                .OrderByDescending(m => m.SentAt)
                .ToList();

            return View(conversations);
        }

        // GET: /Messages/Conversation?userId=5&listingId=1&ticketId=2
        public async Task<IActionResult> Conversation(int userId, int? listingId, int? ticketId)
        {
            var currentUserId = GetCurrentUserId();

            if (userId == currentUserId)
            {
                return BadRequest("Nie możesz napisać wiadomości do siebie.");
            }

            var otherUser = await _context.Users.FindAsync(userId);
            if (otherUser == null)
            {
                return NotFound();
            }

            IQueryable<Message> query = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId));

            if (listingId.HasValue)
            {
                query = query.Where(m => m.ListingId == listingId);
            }

            if (ticketId.HasValue)
            {
                query = query.Where(m => m.TicketId == ticketId);
            }

            var messages = await query
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            foreach (var message in messages)
            {
                if (message.IsArchived)
                {
                    message.Content = "Ta wiadomość została usunięta.";
                    message.Sender = null;
                    message.Receiver = null;
                    message.SentAt = DateTime.MinValue;
                }
            }

            ViewBag.OtherUser = otherUser;
            ViewBag.ListingId = listingId;
            ViewBag.TicketId = ticketId;

            return View(messages);
        }

        // POST: /Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content, int? listingId, int? ticketId)
        {
            var currentUserId = GetCurrentUserId();

            if (receiverId == currentUserId)
            {
                return BadRequest("Nie możesz wysłać wiadomości do siebie.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(nameof(Conversation), new { userId = receiverId, listingId, ticketId });
            }

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Content = content.Trim(),
                ListingId = listingId,
                TicketId = ticketId,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Conversation), new { userId = receiverId, listingId, ticketId });
        }
    }
}


