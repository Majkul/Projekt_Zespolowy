using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IPayuOrderSyncService _payuSync;

        public NotificationsController(MyDBContext context, IPayuOrderSyncService payuSync)
        {
            _context = context;
            _payuSync = payuSync;
        }

        private int GetCurrentUserId()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idString) || !int.TryParse(idString, out var id))
                throw new InvalidOperationException("Brak zalogowanego użytkownika");
            return id;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            await _payuSync.SyncPendingOrdersForSellerAsync(userId);
            var items = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .Include(n => n.Message)!.ThenInclude(m => m!.Sender)
                .Include(n => n.Order)!.ThenInclude(o => o!.Listing)
                .Include(n => n.TradeProposal)!.ThenInclude(t => t!.Initiator)
                .Include(n => n.TradeProposal)!.ThenInclude(t => t!.SubjectListing)
                .ToListAsync();
            return View(items);
        }

        /// <summary>Oznacza powiadomienie jako przeczytane i przekierowuje do powiązanej treści.</summary>
        [HttpGet]
        public async Task<IActionResult> Go(int id)
        {
            var userId = GetCurrentUserId();
            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n == null)
                return NotFound();

            n.IsRead = true;
            await _context.SaveChangesAsync();

            switch (n.Kind)
            {
                case NotificationKind.NewMessage:
                    if (n.MessageId == null)
                        return RedirectToAction(nameof(Index));
                    var m = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == n.MessageId);
                    if (m == null)
                        return RedirectToAction(nameof(Index));
                    return RedirectToAction("Conversation", "Messages", new { userId = m.SenderId, listingId = m.ListingId, ticketId = m.TicketId });

                case NotificationKind.ListingPurchased:
                    if (n.OrderId == null)
                        return RedirectToAction(nameof(Index));
                    var o = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == n.OrderId);
                    if (o == null)
                        return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", "Listings", new { id = o.ListingId });

                case NotificationKind.TradeProposalReceived:
                    if (n.TradeProposalId == null)
                        return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", "TradeProposals", new { id = n.TradeProposalId });

                default:
                    return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetCurrentUserId();
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread)
                n.IsRead = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
