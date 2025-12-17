using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using System.Net;
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
        private readonly IConfiguration _configuration;

        private readonly string _payUBaseUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _merchantPosId;

        public PaymentController(MyDBContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            _payUBaseUrl = configuration["PayU:BaseUrl"];
            _clientId = configuration["PayU:ClientId"];
            _clientSecret = configuration["PayU:ClientSecret"];
            _merchantPosId = configuration["PayU:MerchantPosId"];
        }

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
        public async Task<IActionResult> Notify()
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

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PayUOrderId == payuOrderId);

            if (order == null)
                return Ok();

            if (payuStatus == "COMPLETED" && order.Status != OrderStatus.Paid)
            {
                order.Status = OrderStatus.Paid;

                var listing = await _context.Listings.FindAsync(order.ListingId);
                if (listing != null)
                    listing.IsSold = true;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (order.BuyerId != userId && !User.IsInRole("Admin"))
                return Forbid();

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
            return doc.RootElement.GetProperty("access_token").GetString();
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
                $"{_payUBaseUrl}/api/v2_1/orders",
                content);

            if (response.StatusCode != HttpStatusCode.Found)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"PayU error {response.StatusCode}: {err}");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var redirectUri = doc.RootElement.GetProperty("redirectUri").GetString();
            var payuOrderId = doc.RootElement.GetProperty("orderId").GetString();

            order.PayUOrderId = payuOrderId;
            await _context.SaveChangesAsync();

            return redirectUri;
        }
    }
}
