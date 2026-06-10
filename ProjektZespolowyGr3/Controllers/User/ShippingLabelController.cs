using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class ShippingLabelController : Controller
    {
        private readonly MyDBContext _context;

        public ShippingLabelController(MyDBContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static ShippingParty BuildParty(ProjektZespolowyGr3.Models.DbModels.User user, string fallbackName)
        {
            var name = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
                ? user.Username
                : $"{user.FirstName} {user.LastName}".Trim();

            return new ShippingParty
            {
                Name = string.IsNullOrWhiteSpace(name) ? fallbackName : name,
                Address = string.IsNullOrWhiteSpace(user.Address) ? null : user.Address,
                Phone = user.PhoneNumber,
                Email = user.Email
            };
        }

        [HttpGet]
        public async Task<IActionResult> Order(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Listing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && order.BuyerId != userId && order.SellerId != userId)
                return Forbid();

            var seller = await _context.Users.FindAsync(order.SellerId);
            var buyer  = await _context.Users.FindAsync(order.BuyerId);

            if (seller == null || buyer == null)
                return NotFound();

            var vm = new ShippingLabelViewModel
            {
                From           = BuildParty(seller, "Sprzedawca"),
                To             = BuildParty(buyer, "Kupujący"),
                Contents       = order.Listing != null
                    ? $"{order.Listing.Title} (szt. {order.Quantity})"
                    : $"Zamówienie #{order.Id}",
                ReferenceNumber = $"ZAM-{order.Id:D6}",
                Date           = order.CreatedAt,
                LabelType      = "Zamówienie"
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Trade(int tradeId)
        {
            var proposal = await _context.TradeProposals
                .Include(p => p.Initiator)
                .Include(p => p.Receiver)
                .Include(p => p.Items).ThenInclude(i => i.Listing)
                .Include(p => p.SubjectListing)
                .FirstOrDefaultAsync(p => p.Id == tradeId);

            if (proposal == null)
                return NotFound();

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && proposal.InitiatorUserId != userId && proposal.ReceiverUserId != userId)
                return Forbid();

            if (proposal.Status != TradeProposalStatus.Accepted && !isAdmin)
                return BadRequest("Etykiety można generować tylko dla zaakceptowanych wymian.");

            var initiatorItems = proposal.Items
                .Where(i => i.Side == TradeProposalSide.Initiator && i.Listing != null)
                .Select(i => $"{i.Listing!.Title} (szt. {Math.Max(1, i.Quantity)})")
                .ToList();

            var receiverItems = proposal.Items
                .Where(i => i.Side == TradeProposalSide.Receiver && i.Listing != null)
                .Select(i => $"{i.Listing!.Title} (szt. {Math.Max(1, i.Quantity)})")
                .ToList();

            var initiatorContents = initiatorItems.Any()
                ? string.Join(", ", initiatorItems)
                : $"Wymiana #{proposal.Id}";

            var receiverContents = receiverItems.Any()
                ? string.Join(", ", receiverItems)
                : $"Wymiana #{proposal.Id}";

            var labelA = new ShippingLabelViewModel
            {
                From           = BuildParty(proposal.Initiator, "Składający ofertę"),
                To             = BuildParty(proposal.Receiver, "Otrzymujący ofertę"),
                Contents       = initiatorContents,
                ReferenceNumber = $"WYM-{proposal.Id:D6}-A",
                Date           = proposal.UpdatedAt,
                LabelType      = "Wymiana"
            };

            var labelB = new ShippingLabelViewModel
            {
                From           = BuildParty(proposal.Receiver, "Otrzymujący ofertę"),
                To             = BuildParty(proposal.Initiator, "Składający ofertę"),
                Contents       = receiverContents,
                ReferenceNumber = $"WYM-{proposal.Id:D6}-B",
                Date           = proposal.UpdatedAt,
                LabelType      = "Wymiana"
            };

            var vm = new TradeLabelViewModel
            {
                LabelA          = labelA,
                LabelB          = labelB,
                TradeProposalId = proposal.Id
            };

            return View(vm);
        }
    }
}
