using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IPayuOrderSyncService _payuSync;
        private readonly ILogger<PaymentController> _logger;

        private readonly string _payUBaseUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _merchantPosId;

        public PaymentController(
            MyDBContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IPayuOrderSyncService payuSync,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _payuSync = payuSync;
            _logger = logger;

            _payUBaseUrl = configuration["PayU:BaseUrl"] ?? "";
            _clientId = configuration["PayU:ClientId"] ?? "";
            _clientSecret = configuration["PayU:ClientSecret"] ?? "";
            _merchantPosId = configuration["PayU:MerchantPosId"] ?? "";
        }

        [HttpPost]
        public async Task<IActionResult> Buy(int listingId, int quantity = 1, int? shippingOptionId = null)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var listing = await _context.Listings
                .Include(l => l.ShippingOptions)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null || listing.IsArchived || !ListingStockHelper.CanSell(listing, quantity))
                return BadRequest("Oferta nie istnieje, jest niedostępna lub podano złą ilość.");

            if (!listing.Price.HasValue)
                return BadRequest("Ta oferta nie jest na sprzedaż.");

            if (listing.SellerId == userId)
                return BadRequest("Nie można kupić własnej oferty");

            var unitPrice = listing.Price!.Value;
            decimal shippingCost = 0;
            string? shippingName = null;

            if (shippingOptionId.HasValue)
            {
                var opt = listing.ShippingOptions.FirstOrDefault(o => o.Id == shippingOptionId.Value);
                if (opt != null)
                {
                    shippingCost = opt.Price;
                    shippingName = opt.Name;
                }
            }
            else if (listing.ShippingOptions.Any())
            {
                // Seller has shipping options but buyer didn't pick one — redirect back
                TempData["BuyError"] = "Proszę wybrać metodę dostawy przed zakupem.";
                return RedirectToAction("Details", "Listings", new { id = listingId });
            }

            var order = new Order
            {
                ListingId = listing.Id,
                BuyerId = userId,
                SellerId = listing.SellerId,
                Amount = unitPrice * quantity + shippingCost,
                Quantity = quantity,
                SelectedShippingName = shippingName,
                ShippingCost = shippingCost,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                PayUOrderId = ""
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var token = await GetPayUTokenAsync();
            var redirectUrl = await CreatePayUOrderAsync(order, listing, token);

            return Redirect(redirectUrl);
        }

        [HttpPost]
        public async Task<IActionResult> BuyTradeOrder(int tradeId, int? shippingOptionId = null)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var trade = await _context.TradeProposals
                .Include(p => p.Initiator)
                .Include(p => p.Receiver)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Listing!)
                        .ThenInclude(l => l!.ShippingOptions)
                .FirstOrDefaultAsync(p => p.Id == tradeId);

            if (trade == null)
                return NotFound();
            if (trade.Status != TradeProposalStatus.Accepted)
                return BadRequest("Wymiana nie została zaakceptowana.");
            if (trade.InitiatorUserId != userId && trade.ReceiverUserId != userId)
                return Forbid();

            var payerSide = trade.InitiatorUserId == userId
                ? TradeProposalSide.Initiator
                : TradeProposalSide.Receiver;

            var receiverUserId = payerSide == TradeProposalSide.Initiator
                ? trade.ReceiverUserId
                : trade.InitiatorUserId;

            // Check if TradeOrder already exists for this side
            var existingOrder = await _context.TradeOrders
                .FirstOrDefaultAsync(o => o.TradeProposalId == tradeId && o.PayerSide == payerSide);
            if (existingOrder != null && existingOrder.Status == OrderStatus.Paid)
            {
                TempData["TradePaymentInfo"] = "Płatność za tę stronę wymiany została już zrealizowana.";
                return RedirectToAction("Details", "TradeProposals", new { id = tradeId });
            }

            // Calculate cash supplement from payer's side items
            var cashAmount = trade.Items
                .Where(i => i.Side == payerSide && i.CashAmount.HasValue)
                .Sum(i => i.CashAmount!.Value);

            // Collect all shipping options from payer's listing items
            var payerListings = trade.Items
                .Where(i => i.Side == payerSide && i.Listing != null)
                .Select(i => i.Listing!)
                .ToList();
            var allShippingOptions = payerListings.SelectMany(l => l.ShippingOptions).ToList();

            decimal shippingCost = 0;
            string? shippingName = null;

            if (shippingOptionId.HasValue)
            {
                var opt = allShippingOptions.FirstOrDefault(o => o.Id == shippingOptionId.Value);
                if (opt != null)
                {
                    shippingCost = opt.Price;
                    shippingName = opt.Name;
                }
            }
            else if (allShippingOptions.Any())
            {
                TempData["TradePaymentError"] = "Proszę wybrać metodę dostawy.";
                return RedirectToAction("Details", "TradeProposals", new { id = tradeId });
            }

            var totalAmount = cashAmount + shippingCost;
            if (totalAmount <= 0)
            {
                TempData["TradePaymentError"] = "Brak kwoty do zapłaty — dopłata gotówkowa i koszt dostawy wynoszą 0 PLN.";
                return RedirectToAction("Details", "TradeProposals", new { id = tradeId });
            }

            var tradeOrder = existingOrder ?? new TradeOrder();
            tradeOrder.TradeProposalId = tradeId;
            tradeOrder.PayerUserId = userId;
            tradeOrder.ReceiverUserId = receiverUserId;
            tradeOrder.PayerSide = payerSide;
            tradeOrder.CashAmount = cashAmount;
            tradeOrder.ShippingCost = shippingCost;
            tradeOrder.TotalAmount = totalAmount;
            tradeOrder.SelectedShippingName = shippingName;
            tradeOrder.Status = OrderStatus.Pending;
            tradeOrder.CreatedAt = DateTime.UtcNow;
            tradeOrder.PayUOrderId = "";

            if (existingOrder == null)
                _context.TradeOrders.Add(tradeOrder);

            await _context.SaveChangesAsync();

            var token = await GetPayUTokenAsync();
            var redirectUrl = await CreatePayUOrderForTradeAsync(tradeOrder, trade, token);
            return Redirect(redirectUrl);
        }

        [HttpGet]
        public async Task<IActionResult> TradeOrderSuccess(int tradeOrderId)
        {
            var tradeOrder = await _context.TradeOrders
                .Include(o => o.TradeProposal)
                .FirstOrDefaultAsync(o => o.Id == tradeOrderId);
            if (tradeOrder == null)
                return NotFound();

            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();
            if (tradeOrder.PayerUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            await _payuSync.TryFinalizeTradeOrderFromPayuApiAsync(tradeOrder.Id);
            await _context.Entry(tradeOrder).ReloadAsync();

            return View(tradeOrder);
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Notify()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(body);

                var orderEl = doc.RootElement.GetProperty("order");
                var payuOrderId = orderEl.GetProperty("orderId").GetString();
                var payuStatus = orderEl.GetProperty("status").GetString();

                string? cardToken = null;
                string? cardMasked = null;
                string? cardBrand = null;
                int cardExpiryMonth = 0, cardExpiryYear = 0;

                if (orderEl.TryGetProperty("payMethod", out var pm))
                {
                    if (pm.TryGetProperty("cardToken", out var ct)) cardToken = ct.GetString();
                    if (pm.TryGetProperty("card", out var card))
                    {
                        if (card.TryGetProperty("cardNumberMasked", out var cn)) cardMasked = cn.GetString();
                        if (card.TryGetProperty("brand", out var br)) cardBrand = br.GetString();
                        if (card.TryGetProperty("expirationMonth", out var em) && int.TryParse(em.GetString(), out var emv))
                            cardExpiryMonth = emv;
                        if (card.TryGetProperty("expirationYear", out var ey) && int.TryParse(ey.GetString(), out var eyv))
                            cardExpiryYear = eyv;
                    }
                }

                await _payuSync.HandleNotifyAsync(payuOrderId ?? "", payuStatus,
                    cardToken, cardMasked, cardBrand, cardExpiryMonth, cardExpiryYear);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PayU Notify: błąd przetwarzania webhooka");
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();
            if (order.BuyerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            await _payuSync.TryFinalizeOrderFromPayuApiAsync(order.Id);
            await _context.Entry(order).ReloadAsync();
            await _payuSync.EnsureListingPurchasedNotificationIfNeededAsync(order.Id);

            return View(order);
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }

        private async Task<string> GetPayUTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            });

            var response = await client.PostAsync(
                $"{_payUBaseUrl}/pl/standard/user/oauth/authorize",
                content);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayU OAuth error: {json}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("PayU OAuth: brak access_token w odpowiedzi.");
        }

        private async Task<string> CreatePayUOrderForTradeAsync(TradeOrder tradeOrder, TradeProposal trade, string token)
        {
            var client = _httpClientFactory.CreateClient("PayU");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var notifyUrl = "https://185.238.72.248/Payment/Notify";

            var products = new List<object>();
            if (tradeOrder.CashAmount > 0)
                products.Add(new
                {
                    name = $"Dopłata do wymiany #{trade.Id}",
                    unitPrice = ((int)(tradeOrder.CashAmount * 100)).ToString(),
                    quantity = "1"
                });
            if (tradeOrder.ShippingCost > 0)
                products.Add(new
                {
                    name = $"Wysyłka — {tradeOrder.SelectedShippingName ?? "dostawa"}",
                    unitPrice = ((int)(tradeOrder.ShippingCost * 100)).ToString(),
                    quantity = "1"
                });

            var payload = new
            {
                notifyUrl = notifyUrl,
                continueUrl = Url.Action("TradeOrderSuccess", "Payment",
                    new { tradeOrderId = tradeOrder.Id }, Request.Scheme),
                customerIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                merchantPosId = _merchantPosId,
                description = $"Płatność za wymianę #{trade.Id}",
                currencyCode = "PLN",
                totalAmount = ((int)(tradeOrder.TotalAmount * 100)).ToString(),
                buyer = new
                {
                    email = User.FindFirstValue(ClaimTypes.Email),
                    firstName = "Jan",
                    lastName = "Kowalski",
                    language = "pl"
                },
                products = products.ToArray()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_payUBaseUrl}/api/v2_1/orders", content);

            if (response.StatusCode != HttpStatusCode.Found)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"PayU error {response.StatusCode}: {err}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var redirectUri = doc.RootElement.GetProperty("redirectUri").GetString()
                ?? throw new InvalidOperationException("PayU: brak redirectUri.");
            var payuOrderId = doc.RootElement.GetProperty("orderId").GetString() ?? "";

            tradeOrder.PayUOrderId = payuOrderId;
            await _context.SaveChangesAsync();

            return redirectUri;
        }

        private async Task<string> CreatePayUOrderAsync(Order order, Listing listing, string token)
        {
            var client = _httpClientFactory.CreateClient("PayU");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            //To webhook powinien działać w produkcji, bo lokalnie ciężko to testować
            var notifyUrl = "https://185.238.72.248/Payment/Notify";

            var payload = new
            {
                notifyUrl = notifyUrl,
                continueUrl = Url.Action("Success", "Payment",
                    new { orderId = order.Id }, Request.Scheme),

                customerIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                merchantPosId = _merchantPosId,
                description = $"Zakup: {listing.Title}",
                currencyCode = "PLN",
                totalAmount = ((int)(order.Amount * 100)).ToString(),

                //To do zmiany potem żeby odczytywać usera co kupuje
                buyer = new
                {
                    email = User.FindFirstValue(ClaimTypes.Email),
                    firstName = "Jan",
                    lastName = "Kowalski",
                    language = "pl"
                },
                //Tutaj zrobić odczytywanie z listingu
                products = new[]
                {
                    new
                    {
                        name = listing.Title,
                        unitPrice = ((int)Math.Round((double)(listing.Price!.Value * 100), MidpointRounding.AwayFromZero)).ToString(),
                        quantity = Math.Max(1, order.Quantity).ToString()
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"{_payUBaseUrl}/api/v2_1/orders",
                content);

            if (response.StatusCode != HttpStatusCode.Found)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"PayU error {response.StatusCode}: {err}");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var redirectUri = doc.RootElement.GetProperty("redirectUri").GetString()
                ?? throw new InvalidOperationException("PayU: brak redirectUri w odpowiedzi.");
            var payuOrderId = doc.RootElement.GetProperty("orderId").GetString() ?? "";

            order.PayUOrderId = payuOrderId;
            await _context.SaveChangesAsync();

            return redirectUri;
        }
    }
}
