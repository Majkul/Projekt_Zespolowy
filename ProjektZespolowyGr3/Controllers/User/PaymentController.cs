using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        // 🔹 PAYU SANDBOX
        private const string PayUBaseUrl = "https://secure.snd.payu.com";
        private const string ClientId = "502409";
        private const string ClientSecret = "413c31c49208388040c27c24de1fc04d";
        private const string MerchantPosId = "502409";

        public PaymentController(MyDBContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // ===================== BUY =====================
        [HttpPost]
        public async Task<IActionResult> Buy(int listingId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var listing = await _context.Listings.FindAsync(listingId);

            if (listing == null || listing.IsSold)
                return BadRequest("Listing nie istnieje lub jest sprzedany");

            var order = new Order
            {
                ListingId = listing.Id,
                BuyerId = userId,
                SellerId = listing.SellerId,
                Amount = (decimal)listing.Price,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var token = await GetPayUTokenAsync();
            var redirectUrl = await CreatePayUOrderAsync(order, listing, token);

            return Redirect(redirectUrl);
        }

        // ===================== SUCCESS =====================
        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (order.BuyerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            if (order.Status != OrderStatus.Paid)
            {
                order.Status = OrderStatus.Paid;

                var listing = await _context.Listings.FindAsync(order.ListingId);
                if (listing != null)
                    listing.IsSold = true;

                await _context.SaveChangesAsync();
            }

            return View(order);
        }

        // ===================== CANCEL =====================
        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }

        // ===================== OAUTH TOKEN =====================
        private async Task<string> GetPayUTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret
            });

            var response = await client.PostAsync(
                $"{PayUBaseUrl}/pl/standard/user/oauth/authorize", content);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayU OAuth error: {json}");

            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new Exception("PayU OAuth returned non-JSON response");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        // ===================== CREATE ORDER =====================
        private async Task<string> CreatePayUOrderAsync(Order order, Listing listing, string token)
        {
            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                notifyUrl = Url.Action("Notify", "Payment", null, Request.Scheme),
                continueUrl = Url.Action("Success", "Payment",
                    new { orderId = order.Id }, Request.Scheme),
                customerIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                merchantPosId = MerchantPosId,
                description = $"Zakup: {listing.Title}",
                currencyCode = "PLN",
                totalAmount = ((int)(order.Amount * 100)).ToString(),
                products = new[]
                {
                    new
                    {
                        name = listing.Title,
                        unitPrice = ((int)(order.Amount * 100)).ToString(),
                        quantity = "1"
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"{PayUBaseUrl}/api/v2_1/orders", content);

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayU API error: {responseString}");

            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new Exception("PayU returned HTML instead of JSON");

            using var doc = JsonDocument.Parse(responseString);

            var redirectUri = doc.RootElement.GetProperty("redirectUri").GetString();
            var payuOrderId = doc.RootElement.GetProperty("orderId").GetString();

            order.PayUOrderId = payuOrderId;
            await _context.SaveChangesAsync();

            return redirectUri;
        }
    }
}
