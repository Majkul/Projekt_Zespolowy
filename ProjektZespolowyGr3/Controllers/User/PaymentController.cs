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
        public async Task<IActionResult> Buy(int listingId, int quantity = 1)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();
            var listing = await _context.Listings.FindAsync(listingId);

            if (listing == null || listing.IsArchived || !ListingStockHelper.CanSell(listing, quantity))
                return BadRequest("Oferta nie istnieje, jest niedostępna lub podano złą ilość.");

            if (listing.Type != ListingType.Sale || !listing.Price.HasValue)
                return BadRequest("Ta oferta nie jest na sprzedaż.");

            // wlasne
            if (listing.SellerId == userId)
            {
                return BadRequest("Nie można kupić własnej oferty");
            }

            var unitPrice = listing.Price!.Value;
            var order = new Order
            {
                ListingId = listing.Id,
                BuyerId = userId,
                SellerId = listing.SellerId,
                Amount = unitPrice * quantity,
                Quantity = quantity,
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
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Notify()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(body);

                var payuOrderId = doc.RootElement
                    .GetProperty("order")
                    .GetProperty("orderId")
                    .GetString();

                var payuStatus = doc.RootElement
                    .GetProperty("order")
                    .GetProperty("status")
                    .GetString();

                await _payuSync.HandleNotifyAsync(payuOrderId ?? "", payuStatus);
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
