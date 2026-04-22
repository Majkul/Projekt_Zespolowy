using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;
using System.Globalization;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class TradeProposalsController : Controller
    {
        private readonly MyDBContext _context;
        private readonly INotificationService _notifications;

        public TradeProposalsController(MyDBContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        private int GetCurrentUserId()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idString) || !int.TryParse(idString, out var id))
                throw new InvalidOperationException("Brak zalogowanego użytkownika");
            return id;
        }

        /// <summary>
        /// ID korzenia łańcucha kontrofert (propozycja bez rodzica).
        /// </summary>
        private async Task<int> ResolveTradeThreadRootIdAsync(int proposalId)
        {
            var id = proposalId;
            while (true)
            {
                var row = await _context.TradeProposals.AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new { p.Id, p.ParentTradeProposalId })
                    .FirstOrDefaultAsync();
                if (row == null)
                    return proposalId;
                if (row.ParentTradeProposalId == null)
                    return row.Id;
                id = row.ParentTradeProposalId.Value;
            }
        }

        private static decimal EstimateListingValue(Listing l) => l.Price ?? 0;

        private static decimal SumSide(IEnumerable<TradeProposalItem> items, TradeProposalSide side, Dictionary<int, Listing> listingMap)
        {
            decimal s = 0;
            foreach (var it in items.Where(i => i.Side == side))
            {
                if (it.ListingId.HasValue && listingMap.TryGetValue(it.ListingId.Value, out var listing))
                    s += EstimateListingValue(listing) * Math.Max(1, it.Quantity);
                if (it.CashAmount.HasValue)
                    s += it.CashAmount.Value;
            }
            return s;
        }

        private static int ResolvePostedQuantity(Dictionary<int, int>? posted, int listingId)
        {
            if (posted != null && posted.TryGetValue(listingId, out var q) && q > 0)
                return q;
            return 1;
        }

        /// <summary>
        /// Odczytuje np. initiatorQuantities[42]=3 z formularza — pomija nieprawidłowe klucze (binder Dictionary int,int zgłaszałby FormatException).
        /// </summary>
        private static Dictionary<int, int> ParseListingQuantitiesFromForm(IFormCollection form, string prefix)
        {
            var result = new Dictionary<int, int>();
            var head = prefix + "[";
            foreach (var kv in form)
            {
                var k = kv.Key;
                if (k.Length <= head.Length || !k.StartsWith(head, StringComparison.OrdinalIgnoreCase))
                    continue;
                var endBracket = k.IndexOf(']', head.Length);
                if (endBracket < 0)
                    continue;
                var idSegment = k.AsSpan(head.Length, endBracket - head.Length);
                if (!int.TryParse(idSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var listingId))
                    continue;
                var raw = kv.Value.ToString();
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty) && qty > 0)
                    result[listingId] = qty;
            }

            return result;
        }

        [HttpGet]
        public async Task<IActionResult> Compose(int listingId, int? editTradeProposalId, int? parentTradeProposalId)
        {
            var userId = GetCurrentUserId();
            var subject = await _context.Listings
                .Include(l => l.Seller)
                .Include(l => l.Photos).ThenInclude(p => p.Upload)
                .Include(l => l.ExchangeAcceptedTags).ThenInclude(e => e.Tag)
                .Include(l => l.Tags).ThenInclude(t => t.Tag)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (subject == null)
                return NotFound();

            if (subject.NotExchangeable || subject.IsArchived || !ListingStockHelper.IsAvailableForTrade(subject))
            {
                TempData["TradeError"] = "To ogłoszenie nie jest dostępne do wymiany.";
                return RedirectToAction("Details", "Listings", new { id = listingId });
            }

            TradeProposal? edit = null;
            TradeProposal? parent = null;

            if (editTradeProposalId.HasValue)
            {
                edit = await LoadProposalFull(editTradeProposalId.Value);
                if (edit == null || edit.Status != TradeProposalStatus.Pending)
                    return NotFound();
                if (edit.InitiatorUserId != userId)
                    return Forbid();
                if (edit.SubjectListingId != listingId)
                    return BadRequest();
            }

            if (parentTradeProposalId.HasValue)
            {
                parent = await LoadProposalFull(parentTradeProposalId.Value);
                if (parent == null || parent.Status != TradeProposalStatus.Pending)
                    return NotFound();
            }

            int initiatorId;
            int receiverId;

            if (parent != null)
            {
                if (parent.ReceiverUserId != userId && parent.InitiatorUserId != userId)
                    return Forbid();
                initiatorId = userId;
                receiverId = parent.InitiatorUserId == userId ? parent.ReceiverUserId : parent.InitiatorUserId;
            }
            else if (edit != null)
            {
                initiatorId = edit.InitiatorUserId;
                receiverId = edit.ReceiverUserId;
            }
            else
            {
                if (subject.SellerId == userId)
                {
                    TempData["TradeError"] = "Nie możesz wymienić się sam ze sobą.";
                    return RedirectToAction("Details", "Listings", new { id = listingId });
                }
                initiatorId = userId;
                receiverId = subject.SellerId;
            }

            var initiatorPool = await QueryPool(initiatorId);
            var receiverPool = await QueryPool(receiverId);

            var initiatorUser = await _context.Users.FindAsync(initiatorId);
            var receiverUser = await _context.Users.FindAsync(receiverId);
            var initiatorUsername = initiatorUser?.Username ?? "?";
            var receiverUsername = receiverUser?.Username ?? "?";

            var vm = new ComposeTradeViewModel
            {
                SubjectListingId = listingId,
                SubjectListing = subject,
                EditTradeProposalId = editTradeProposalId,
                ParentTradeProposalId = parentTradeProposalId,
                InitiatorPool = initiatorPool,
                ReceiverPool = receiverPool,
                ParentProposal = parent,
                InitiatorUsername = initiatorUsername,
                ReceiverUsername = receiverUsername,
                BuyerOffersFromInitiatorColumn = subject.SellerId == receiverId
            };

            if (edit != null)
            {
                vm.SelectedInitiatorListingIds = edit.Items.Where(i => i.Side == TradeProposalSide.Initiator && i.ListingId.HasValue).Select(i => i.ListingId!.Value).Distinct().ToList();
                vm.SelectedReceiverListingIds = edit.Items.Where(i => i.Side == TradeProposalSide.Receiver && i.ListingId.HasValue).Select(i => i.ListingId!.Value).Distinct().ToList();
                vm.InitiatorQuantities = edit.Items
                    .Where(i => i.Side == TradeProposalSide.Initiator && i.ListingId.HasValue)
                    .GroupBy(i => i.ListingId!.Value)
                    .ToDictionary(g => g.Key, g => Math.Max(1, g.Sum(x => x.Quantity)));
                vm.ReceiverQuantities = edit.Items
                    .Where(i => i.Side == TradeProposalSide.Receiver && i.ListingId.HasValue)
                    .GroupBy(i => i.ListingId!.Value)
                    .ToDictionary(g => g.Key, g => Math.Max(1, g.Sum(x => x.Quantity)));
                vm.InitiatorCash = edit.Items.Where(i => i.Side == TradeProposalSide.Initiator).Sum(i => i.CashAmount ?? 0);
                vm.ReceiverCash = edit.Items.Where(i => i.Side == TradeProposalSide.Receiver).Sum(i => i.CashAmount ?? 0);
            }
            else if (parent != null)
            {
                if (subject.SellerId == initiatorId)
                {
                    vm.SelectedInitiatorListingIds = new List<int> { subject.Id };
                    vm.InitiatorQuantities[subject.Id] = 1;
                }
                else
                {
                    vm.SelectedReceiverListingIds = new List<int> { subject.Id };
                    vm.ReceiverQuantities[subject.Id] = 1;
                }
            }
            else
            {
                vm.SelectedReceiverListingIds = new List<int> { subject.Id };
                vm.ReceiverQuantities[subject.Id] = 1;
            }

            return View(vm);
        }

        private async Task<List<Listing>> QueryPool(int sellerId)
        {
            return await _context.Listings
                .Include(l => l.Photos).ThenInclude(p => p.Upload)
                .Include(l => l.Tags).ThenInclude(t => t.Tag)
                .Where(l => l.SellerId == sellerId && !l.IsArchived && !l.NotExchangeable && l.StockQuantity > 0 && !l.IsSold)
                .OrderByDescending(l => l.UpdatedAt)
                .ToListAsync();
        }

        private async Task<TradeProposal?> LoadProposalFull(int id)
        {
            return await _context.TradeProposals
                .Include(p => p.Initiator)
                .Include(p => p.Receiver)
                .Include(p => p.Items).ThenInclude(i => i.Listing!)
                    .ThenInclude(l => l!.Photos).ThenInclude(ph => ph.Upload)
                .Include(p => p.Items).ThenInclude(i => i.Listing!)
                    .ThenInclude(l => l!.Tags).ThenInclude(t => t.Tag)
                .Include(p => p.SubjectListing!).ThenInclude(s => s.ExchangeAcceptedTags).ThenInclude(e => e.Tag)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int subjectListingId,
            List<int>? initiatorListingIds,
            List<int>? receiverListingIds,
            decimal initiatorCash,
            decimal receiverCash,
            int? editTradeProposalId,
            int? parentTradeProposalId)
        {
            var userId = GetCurrentUserId();
            initiatorListingIds ??= new List<int>();
            receiverListingIds ??= new List<int>();
            var initiatorQuantities = ParseListingQuantitiesFromForm(Request.Form, "initiatorQuantities");
            var receiverQuantities = ParseListingQuantitiesFromForm(Request.Form, "receiverQuantities");

            if (editTradeProposalId.HasValue)
            {
                var existing = await _context.TradeProposals
                    .Include(p => p.Items)
                    .Include(p => p.SubjectListing!).ThenInclude(s => s.ExchangeAcceptedTags).ThenInclude(e => e.Tag)
                    .FirstOrDefaultAsync(p => p.Id == editTradeProposalId.Value);
                if (existing == null || existing.Status != TradeProposalStatus.Pending || existing.InitiatorUserId != userId)
                    return Forbid();
                if (existing.SubjectListingId != subjectListingId)
                    return BadRequest();

                var subject = existing.SubjectListing!;
                if (subject.NotExchangeable || subject.IsArchived || !ListingStockHelper.IsAvailableForTrade(subject))
                {
                    TempData["TradeError"] = "Ogłoszenie niedostępne do wymiany.";
                    return RedirectToAction("Compose", new { listingId = subjectListingId, editTradeProposalId });
                }

                var editInitiatorId = existing.InitiatorUserId;
                var editReceiverId = existing.ReceiverUserId;
                var editResult = await ValidateAndBuildSides(
                    subject, editInitiatorId, editReceiverId, initiatorListingIds, receiverListingIds, initiatorQuantities, receiverQuantities, initiatorCash, receiverCash);
                if (editResult.Error != null)
                {
                    TempData["TradeError"] = editResult.Error;
                    return RedirectToAction("Compose", new { listingId = subjectListingId, editTradeProposalId });
                }

                var editUtc = DateTime.UtcNow;
                existing.Items.Clear();
                AddItems(existing, editResult, initiatorCash, receiverCash);
                existing.UpdatedAt = editUtc;
                existing.LastModifiedAt = editUtc;
                _context.TradeProposalHistoryEntries.Add(new TradeProposalHistoryEntry
                {
                    TradeProposal = existing,
                    UserId = userId,
                    ChangedAt = editUtc,
                    Summary = "Zmodyfikowano propozycję wymiany."
                });
                await _context.SaveChangesAsync();
                var otherUserId = existing.ReceiverUserId == userId ? existing.InitiatorUserId : existing.ReceiverUserId;
                return RedirectToAction("Conversation", "Messages", new { userId = otherUserId, listingId = subject.Id });
            }

            var subjectNew = await _context.Listings
                .Include(l => l.ExchangeAcceptedTags).ThenInclude(e => e.Tag)
                .Include(l => l.Tags).ThenInclude(t => t.Tag)
                .FirstOrDefaultAsync(l => l.Id == subjectListingId);

            if (subjectNew == null)
                return NotFound();

            if (subjectNew.NotExchangeable || subjectNew.IsArchived || !ListingStockHelper.IsAvailableForTrade(subjectNew))
            {
                TempData["TradeError"] = "Ogłoszenie niedostępne do wymiany.";
                return RedirectToAction("Compose", new { listingId = subjectListingId });
            }

            TradeProposal? parent = null;
            Message? parentMessage = null;
            if (parentTradeProposalId.HasValue)
            {
                parent = await LoadProposalFull(parentTradeProposalId.Value);
                if (parent == null || parent.Status != TradeProposalStatus.Pending)
                    return NotFound();
                if (parent.InitiatorUserId != userId && parent.ReceiverUserId != userId)
                    return Forbid();
                if (parent.SubjectListingId != subjectListingId)
                {
                    TempData["TradeError"] = "Niezgodny kontekst ogłoszenia względem propozycji nadrzędnej.";
                    return RedirectToAction("Compose", new { listingId = subjectListingId });
                }
                parentMessage = await _context.Messages
                    .OrderBy(m => m.Id)
                    .FirstOrDefaultAsync(m => m.TradeProposalId == parent.Id);
            }

            if (parent == null && subjectNew.SellerId == userId)
            {
                TempData["TradeError"] = "Pierwszą propozycję wymiany składa kupujący.";
                return RedirectToAction("Details", "Listings", new { id = subjectListingId });
            }

            int initiatorId;
            int receiverId;
            if (parent != null)
            {
                initiatorId = userId;
                receiverId = parent.InitiatorUserId == userId ? parent.ReceiverUserId : parent.InitiatorUserId;
            }
            else
            {
                initiatorId = userId;
                receiverId = subjectNew.SellerId;
            }

            if (initiatorId == receiverId)
            {
                TempData["TradeError"] = "Nieprawidłowa konfiguracja stron wymiany.";
                return RedirectToAction("Compose", new { listingId = subjectListingId });
            }

            var sidesResult = await ValidateAndBuildSides(
                subjectNew, initiatorId, receiverId, initiatorListingIds, receiverListingIds, initiatorQuantities, receiverQuantities, initiatorCash, receiverCash);
            if (sidesResult.Error != null)
            {
                TempData["TradeError"] = sidesResult.Error;
                return RedirectToAction("Compose", new { listingId = subjectListingId, parentTradeProposalId });
            }

            var now = DateTime.UtcNow;

            var proposal = new TradeProposal
            {
                InitiatorUserId = initiatorId,
                ReceiverUserId = receiverId,
                SubjectListingId = subjectListingId,
                Status = TradeProposalStatus.Pending,
                ParentTradeProposalId = parent?.Id,
                RootTradeProposalId = parent == null ? null : (parent.RootTradeProposalId ?? parent.Id),
                CreatedAt = now,
                UpdatedAt = now,
                LastModifiedAt = now
            };

            AddItems(proposal, sidesResult, initiatorCash, receiverCash);
            _context.TradeProposals.Add(proposal);
            await _context.SaveChangesAsync();

            if (proposal.RootTradeProposalId == null)
            {
                proposal.RootTradeProposalId = proposal.Id;
                await _context.SaveChangesAsync();
            }

            if (parent != null)
            {
                var trackedParent = await _context.TradeProposals.FindAsync(parent.Id);
                if (trackedParent != null)
                {
                    trackedParent.Status = TradeProposalStatus.Superseded;
                    trackedParent.UpdatedAt = now;
                }
            }

            var message = new Message
            {
                SenderId = initiatorId,
                ReceiverId = receiverId,
                ListingId = subjectListingId,
                Content = "📦 Propozycja wymiany",
                TradeProposalId = proposal.Id,
                ReplyToMessageId = parentMessage?.Id,
                SentAt = now
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await _notifications.NotifyTradeProposalAsync(receiverId, proposal.Id);

            return RedirectToAction("Conversation", "Messages", new { userId = receiverId, listingId = subjectListingId });
        }

        private sealed class TradeSidesResult
        {
            public string? Error { get; set; }
            public List<Listing>? InitiatorListings { get; set; }
            public List<Listing>? ReceiverListings { get; set; }
            public Dictionary<int, int> InitiatorQtyByListingId { get; set; } = new();
            public Dictionary<int, int> ReceiverQtyByListingId { get; set; } = new();
        }

        private async Task<TradeSidesResult> ValidateAndBuildSides(
            Listing subject,
            int initiatorId,
            int receiverId,
            List<int> initiatorListingIds,
            List<int> receiverListingIds,
            Dictionary<int, int>? initiatorQuantities,
            Dictionary<int, int>? receiverQuantities,
            decimal initiatorCash,
            decimal receiverCash)
        {
            if (subject.SellerId == initiatorId && !initiatorListingIds.Contains(subject.Id))
                return new TradeSidesResult { Error = "Musisz uwzględnić ogłoszenie, w którego kontekście wysyłasz wymianę (po Twojej stronie)." };

            if (subject.SellerId == receiverId && !receiverListingIds.Contains(subject.Id))
                return new TradeSidesResult { Error = "Musisz uwzględnić ogłoszenie, w którego kontekście wysyłasz wymianę (po stronie rozmówcy)." };

            var initiatorListings = await _context.Listings
                .Include(l => l.Tags).ThenInclude(t => t.Tag)
                .Where(l => initiatorListingIds.Contains(l.Id) && l.SellerId == initiatorId && !l.IsArchived && !l.NotExchangeable && l.StockQuantity > 0 && !l.IsSold)
                .ToListAsync();

            if (initiatorListings.Count != initiatorListingIds.Distinct().Count())
                return new TradeSidesResult { Error = "Nieprawidłowa lista ogłoszeń po Twojej stronie (inicjator)." };

            var receiverListings = await _context.Listings
                .Include(l => l.Tags).ThenInclude(t => t.Tag)
                .Where(l => receiverListingIds.Contains(l.Id) && l.SellerId == receiverId && !l.IsArchived && !l.NotExchangeable && l.StockQuantity > 0 && !l.IsSold)
                .ToListAsync();

            if (receiverListings.Count != receiverListingIds.Distinct().Count())
                return new TradeSidesResult { Error = "Nieprawidłowa lista ogłoszeń po stronie rozmówcy." };

            var initiatorQtyByListingId = new Dictionary<int, int>();
            foreach (var l in initiatorListings)
            {
                var q = ResolvePostedQuantity(initiatorQuantities, l.Id);
                if (q > l.StockQuantity)
                    return new TradeSidesResult { Error = $"Ogłoszenie „{l.Title}”: możesz wybrać co najwyżej {l.StockQuantity} szt." };
                initiatorQtyByListingId[l.Id] = q;
            }

            var receiverQtyByListingId = new Dictionary<int, int>();
            foreach (var l in receiverListings)
            {
                var q = ResolvePostedQuantity(receiverQuantities, l.Id);
                if (q > l.StockQuantity)
                    return new TradeSidesResult { Error = $"Ogłoszenie „{l.Title}”: możesz wybrać co najwyżej {l.StockQuantity} szt." };
                receiverQtyByListingId[l.Id] = q;
            }

            var buyerSideListings = subject.SellerId == receiverId ? initiatorListings : receiverListings;
            var buyerQtyMap = subject.SellerId == receiverId ? initiatorQtyByListingId : receiverQtyByListingId;
            var buyerCash = subject.SellerId == receiverId ? initiatorCash : receiverCash;
            var buyerSum = buyerCash + buyerSideListings.Sum(l => EstimateListingValue(l) * buyerQtyMap[l.Id]);

            if (subject.MinExchangeValue.HasValue && buyerSum < subject.MinExchangeValue.Value)
            {
                var kol = subject.SellerId == receiverId
                    ? "niebieskiej (składający ofertę)"
                    : "szarej (otrzymujący ofertę), gdy kontrofertę składa właściciel ogłoszenia";
                return new TradeSidesResult
                {
                    Error =
                        $"Minimalna wartość od kupującego w tym ogłoszeniu to {subject.MinExchangeValue.Value:C}, " +
                        $"a policzona suma jego zaznaczonych ogłoszeń i dopłaty to {buyerSum:C}. " +
                        $"Liczy się tylko kolumna {kol} — przedmioty zaznaczone u sprzedającego po drugiej stronie nie zwiększają tej sumy. " +
                        $"Brak własnych ogłoszeń? Użyj dopłaty gotówką po stronie kupującego."
                };
            }

            var acceptedTagIds = subject.ExchangeAcceptedTags.Select(e => e.TagId).ToHashSet();
            if (acceptedTagIds.Count > 0)
            {
                foreach (var listing in buyerSideListings)
                {
                    var tagIds = listing.Tags.Select(t => t.TagId).ToHashSet();
                    if (!tagIds.Any(tid => acceptedTagIds.Contains(tid)))
                        return new TradeSidesResult { Error = "Każde ogłoszenie ze strony kupującego musi mieć co najmniej jeden z tagów akceptowanych przez sprzedającego." };
                }
            }

            return new TradeSidesResult
            {
                InitiatorListings = initiatorListings,
                ReceiverListings = receiverListings,
                InitiatorQtyByListingId = initiatorQtyByListingId,
                ReceiverQtyByListingId = receiverQtyByListingId
            };
        }

        private static void AddItems(
            TradeProposal proposal,
            TradeSidesResult sides,
            decimal initiatorCash,
            decimal receiverCash)
        {
            foreach (var l in sides.InitiatorListings!)
            {
                var q = sides.InitiatorQtyByListingId.TryGetValue(l.Id, out var qv) ? Math.Max(1, qv) : 1;
                proposal.Items.Add(new TradeProposalItem { Side = TradeProposalSide.Initiator, ListingId = l.Id, Quantity = q });
            }

            foreach (var l in sides.ReceiverListings!)
            {
                var q = sides.ReceiverQtyByListingId.TryGetValue(l.Id, out var qv) ? Math.Max(1, qv) : 1;
                proposal.Items.Add(new TradeProposalItem { Side = TradeProposalSide.Receiver, ListingId = l.Id, Quantity = q });
            }

            if (initiatorCash > 0)
                proposal.Items.Add(new TradeProposalItem { Side = TradeProposalSide.Initiator, CashAmount = initiatorCash, Quantity = 1 });
            if (receiverCash > 0)
                proposal.Items.Add(new TradeProposalItem { Side = TradeProposalSide.Receiver, CashAmount = receiverCash, Quantity = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = GetCurrentUserId();
            var proposal = await LoadProposalFull(id);
            if (proposal == null)
                return NotFound();
            if (proposal.ReceiverUserId != userId || proposal.Status != TradeProposalStatus.Pending)
                return Forbid();

            var unitsPerListing = proposal.Items
                .Where(i => i.ListingId.HasValue)
                .GroupBy(i => i.ListingId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(i => Math.Max(1, i.Quantity)));

            var listingIds = unitsPerListing.Keys.ToList();
            var listings = await _context.Listings.Where(l => listingIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id);

            foreach (var (lid, need) in unitsPerListing)
            {
                if (!listings.TryGetValue(lid, out var list))
                {
                    TempData["TradeError"] = "Jedno z ogłoszeń w propozycji już nie istnieje.";
                    return RedirectToAction("Details", new { id });
                }

                if (!ListingStockHelper.CanSell(list, need) || list.IsArchived || list.NotExchangeable)
                {
                    TempData["TradeError"] = "Nie można zaakceptować: niewystarczająca liczba sztuk lub ogłoszenie jest niedostępne.";
                    return RedirectToAction("Details", new { id });
                }
            }

            foreach (var (lid, need) in unitsPerListing)
                ListingStockHelper.ApplySale(listings[lid], need);

            proposal.Status = TradeProposalStatus.Accepted;
            proposal.UpdatedAt = DateTime.UtcNow;
            proposal.LastModifiedAt = DateTime.UtcNow;
            _context.TradeProposalHistoryEntries.Add(new TradeProposalHistoryEntry
            {
                TradeProposalId = proposal.Id,
                UserId = userId,
                Summary = "Propozycja zaakceptowana.",
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["TradeSuccess"] = "Propozycja wymiany została zaakceptowana!";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = GetCurrentUserId();
            var proposal = await _context.TradeProposals.FindAsync(id);
            if (proposal == null)
                return NotFound();
            if (proposal.ReceiverUserId != userId || proposal.Status != TradeProposalStatus.Pending)
                return Forbid();

            proposal.Status = TradeProposalStatus.Rejected;
            proposal.UpdatedAt = DateTime.UtcNow;
            proposal.LastModifiedAt = DateTime.UtcNow;
            _context.TradeProposalHistoryEntries.Add(new TradeProposalHistoryEntry
            {
                TradeProposalId = proposal.Id,
                UserId = userId,
                Summary = "Propozycja odrzucona.",
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["TradeInfo"] = "Propozycja wymiany została odrzucona.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetCurrentUserId();
            var proposal = await _context.TradeProposals.FindAsync(id);
            if (proposal == null)
                return NotFound();
            if (proposal.InitiatorUserId != userId || proposal.Status != TradeProposalStatus.Pending)
                return Forbid();

            proposal.Status = TradeProposalStatus.Cancelled;
            proposal.UpdatedAt = DateTime.UtcNow;
            proposal.LastModifiedAt = DateTime.UtcNow;
            _context.TradeProposalHistoryEntries.Add(new TradeProposalHistoryEntry
            {
                TradeProposalId = proposal.Id,
                UserId = userId,
                Summary = "Propozycja anulowana przez składającego.",
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["TradeInfo"] = "Propozycja wymiany została anulowana.";
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Szybka kontroferta — zachowuje te same przedmioty, ale zmienia kwoty dopłat gotówkowych.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCounterOffer(int id, decimal initiatorCash, decimal receiverCash, string? message)
        {
            var userId = GetCurrentUserId();

            var parent = await _context.TradeProposals
                .Include(p => p.Items).ThenInclude(i => i.Listing!)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parent == null)
                return NotFound();
            if (parent.Status != TradeProposalStatus.Pending)
            {
                TempData["TradeError"] = "Nie można złożyć kontroferty — propozycja nie jest już aktywna.";
                return RedirectToAction("Details", new { id });
            }
            if (parent.ReceiverUserId != userId && parent.InitiatorUserId != userId)
                return Forbid();

            // Only the receiver can make a quick counter-offer on a pending proposal
            if (parent.ReceiverUserId != userId)
            {
                TempData["TradeError"] = "Kontrofertę może złożyć tylko osoba, do której skierowano propozycję.";
                return RedirectToAction("Details", new { id });
            }

            initiatorCash = Math.Max(0, initiatorCash);
            receiverCash = Math.Max(0, receiverCash);

            var now = DateTime.UtcNow;

            // In a counter-offer the roles swap: the current receiver becomes the new initiator
            var newProposal = new TradeProposal
            {
                InitiatorUserId = parent.ReceiverUserId,
                ReceiverUserId = parent.InitiatorUserId,
                SubjectListingId = parent.SubjectListingId,
                Status = TradeProposalStatus.Pending,
                ParentTradeProposalId = parent.Id,
                RootTradeProposalId = parent.RootTradeProposalId ?? parent.Id,
                CreatedAt = now,
                UpdatedAt = now,
                LastModifiedAt = now
            };

            // Copy listing items, swap sides to match role swap, skip old cash items
            foreach (var item in parent.Items.Where(i => i.ListingId.HasValue))
            {
                var swappedSide = item.Side == TradeProposalSide.Initiator
                    ? TradeProposalSide.Receiver
                    : TradeProposalSide.Initiator;

                newProposal.Items.Add(new TradeProposalItem
                {
                    ListingId = item.ListingId,
                    Side = swappedSide,
                    Quantity = item.Quantity
                });
            }

            // Add new cash items (using swapped sides: old initiator is now receiver)
            if (initiatorCash > 0)
                newProposal.Items.Add(new TradeProposalItem { Side = TradeProposalSide.Receiver, CashAmount = initiatorCash });
            if (receiverCash > 0)
                newProposal.Items.Add(new TradeProposalItem { Side = TradeProposalSide.Initiator, CashAmount = receiverCash });

            _context.TradeProposals.Add(newProposal);

            // Mark parent as superseded
            parent.Status = TradeProposalStatus.Superseded;
            parent.UpdatedAt = now;
            parent.LastModifiedAt = now;

            _context.TradeProposalHistoryEntries.Add(new TradeProposalHistoryEntry
            {
                TradeProposalId = parent.Id,
                UserId = userId,
                Summary = "Zastąpiona szybką kontrofertą.",
                ChangedAt = now
            });

            await _context.SaveChangesAsync();

            // Fix root id if this is the first in chain
            if (newProposal.RootTradeProposalId == newProposal.Id)
            {
                newProposal.RootTradeProposalId = newProposal.Id;
                await _context.SaveChangesAsync();
            }

            // Send message notification
            if (!string.IsNullOrWhiteSpace(message))
            {
                _context.Messages.Add(new Message
                {
                    SenderId = userId,
                    ReceiverId = parent.InitiatorUserId,
                    ListingId = parent.SubjectListingId,
                    Content = message.Trim(),
                    TradeProposalId = newProposal.Id,
                    SentAt = now
                });
            }

            _context.Messages.Add(new Message
            {
                SenderId = userId,
                ReceiverId = parent.InitiatorUserId,
                ListingId = parent.SubjectListingId,
                Content = "🔄 Kontroferta wymiany",
                TradeProposalId = newProposal.Id,
                SentAt = now
            });

            await _context.SaveChangesAsync();
            await _notifications.NotifyTradeProposalAsync(parent.InitiatorUserId, newProposal.Id);

            TempData["TradeSuccess"] = "Kontroferta została wysłana!";
            return RedirectToAction("Details", new { id = newProposal.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var list = await _context.TradeProposals
                .AsNoTracking()
                .Include(p => p.SubjectListing)
                .Include(p => p.Initiator)
                .Include(p => p.Receiver)
                .Where(p => p.InitiatorUserId == userId || p.ReceiverUserId == userId)
                .ToListAsync();

            static List<TradeProposal> LatestPerListing(IEnumerable<TradeProposal> source) =>
                source
                    .GroupBy(p => p.SubjectListingId)
                    .Select(g => g.OrderByDescending(p => p.LastModifiedAt).First())
                    .OrderByDescending(p => p.LastModifiedAt)
                    .ToList();

            var listingIdsWithPending = list
                .Where(p => p.Status == TradeProposalStatus.Pending)
                .Select(p => p.SubjectListingId)
                .ToHashSet();

            var active = LatestPerListing(list.Where(p => p.Status == TradeProposalStatus.Pending));
            var archived = LatestPerListing(
                list.Where(p =>
                    p.Status != TradeProposalStatus.Pending
                    && !listingIdsWithPending.Contains(p.SubjectListingId)));

            var vm = new TradeProposalsIndexViewModel
            {
                CurrentUserId = userId,
                ActiveOnePerListing = active,
                ArchivedOnePerListing = archived
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var proposal = await _context.TradeProposals
                .Include(p => p.Initiator)
                .Include(p => p.Receiver)
                .Include(p => p.SubjectListing)
                .Include(p => p.Items).ThenInclude(i => i.Listing)!.ThenInclude(l => l!.Photos).ThenInclude(ph => ph.Upload)
                .Include(p => p.Items).ThenInclude(i => i.Listing)!.ThenInclude(l => l!.Tags).ThenInclude(lt => lt.Tag)
                .Include(p => p.Items).ThenInclude(i => i.Listing)!.ThenInclude(l => l!.ShippingOptions)
                .Include(p => p.ParentTradeProposal!).ThenInclude(pt => pt!.Initiator)
                .Include(p => p.ParentTradeProposal!).ThenInclude(pt => pt!.Receiver)
                .Include(p => p.ParentTradeProposal!).ThenInclude(pt => pt!.Items).ThenInclude(i => i.Listing!).ThenInclude(l => l!.Photos).ThenInclude(ph => ph.Upload)
                .Include(p => p.ParentTradeProposal!).ThenInclude(pt => pt!.Items).ThenInclude(i => i.Listing!).ThenInclude(l => l!.Tags).ThenInclude(lt => lt.Tag)
                .Include(p => p.History).ThenInclude(h => h.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
                return NotFound();
            if (proposal.InitiatorUserId != userId && proposal.ReceiverUserId != userId)
                return Forbid();

            var listingIds = proposal.Items.Where(i => i.ListingId.HasValue).Select(i => i.ListingId!.Value);
            var map = await _context.Listings.Where(l => listingIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id);

            ViewBag.InitiatorSum = SumSide(proposal.Items, TradeProposalSide.Initiator, map);
            ViewBag.ReceiverSum = SumSide(proposal.Items, TradeProposalSide.Receiver, map);
            ViewBag.CurrentUserId = userId;

            if (proposal.ParentTradeProposal != null)
            {
                var par = proposal.ParentTradeProposal;
                var parentIds = par.Items.Where(i => i.ListingId.HasValue).Select(i => i.ListingId!.Value);
                var parentMap = await _context.Listings.Where(l => parentIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id);
                ViewBag.ParentInitiatorSum = SumSide(par.Items, TradeProposalSide.Initiator, parentMap);
                ViewBag.ParentReceiverSum = SumSide(par.Items, TradeProposalSide.Receiver, parentMap);
            }

            // Load existing TradeOrders for this proposal
            var tradeOrders = await _context.TradeOrders
                .Where(o => o.TradeProposalId == id)
                .ToListAsync();
            ViewBag.TradeOrders = tradeOrders;

            var rootId = await ResolveTradeThreadRootIdAsync(proposal.Id);
            var thread = await _context.TradeProposals
                .AsNoTracking()
                .Include(p => p.Initiator)
                .Include(p => p.Receiver)
                .Where(p => p.Id == rootId || p.RootTradeProposalId == rootId)
                .OrderBy(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .ToListAsync();

            ViewBag.Thread = thread;
            return View(proposal);
        }
    }
}
