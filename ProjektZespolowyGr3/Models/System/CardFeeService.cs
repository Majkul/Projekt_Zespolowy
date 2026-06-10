using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.System;

public class CardFeeService : ICardFeeService
{
    private const decimal CommissionRate = 0.05m;
    private const decimal ListingFeeAmount = 0.50m;

    private readonly MyDBContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CardFeeService> _logger;

    private readonly string _payUBaseUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _merchantPosId;
    private readonly string _notifyUrl;

    public CardFeeService(
        MyDBContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CardFeeService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _payUBaseUrl = configuration["PayU:BaseUrl"] ?? "";
        _clientId = configuration["PayU:ClientId"] ?? "";
        _clientSecret = configuration["PayU:ClientSecret"] ?? "";
        _merchantPosId = configuration["PayU:MerchantPosId"] ?? "";
        _notifyUrl = configuration["PayU:NotifyUrl"] ?? "https://185.238.72.248/Payment/Notify";
    }

    public async Task<string> CreateTokenizationOrderAsync(int userId, string customerIp, string continueUrl, CancellationToken cancellationToken = default)
    {
        var token = await GetPayUTokenAsync(cancellationToken);
        var client = CreatePayUClient(token);

        var amountGroszy = (int)(ListingFeeAmount * 100);

        var payload = new
        {
            notifyUrl = _notifyUrl,
            continueUrl = continueUrl,
            customerIp = customerIp,
            merchantPosId = _merchantPosId,
            description = "Weryfikacja karty płatniczej",
            currencyCode = "PLN",
            totalAmount = amountGroszy.ToString(),
            recurring = "FIRST",
            payMethods = new
            {
                payMethod = new { type = "CARD" }
            },
            products = new[]
            {
                new { name = "Weryfikacja karty", unitPrice = amountGroszy.ToString(), quantity = "1" }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{_payUBaseUrl}/api/v2_1/orders", content, cancellationToken);

        if (response.StatusCode != global::System.Net.HttpStatusCode.Found && !response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"PayU tokenization order error {response.StatusCode}: {err}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        var redirectUri = doc.RootElement.GetProperty("redirectUri").GetString()
            ?? throw new InvalidOperationException("PayU: brak redirectUri.");
        var payuOrderId = doc.RootElement.GetProperty("orderId").GetString() ?? "";

        var tokenizationOrder = new CardTokenizationOrder
        {
            UserId = userId,
            PayUOrderId = payuOrderId,
            Completed = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.CardTokenizationOrders.Add(tokenizationOrder);
        await _context.SaveChangesAsync(cancellationToken);

        return redirectUri;
    }

    public async Task<(bool Success, string? Error)> TryChargeListingFeeAsync(int sellerUserId, string listingTitle, CancellationToken cancellationToken = default)
    {
        var card = await _context.SellerCards
            .FirstOrDefaultAsync(c => c.UserId == sellerUserId && c.IsActive, cancellationToken);

        if (card == null)
            return (false, "Brak aktywnej karty płatniczej.");

        try
        {
            var token = await GetPayUTokenAsync(cancellationToken);
            var client = CreatePayUClient(token);

            var amountGroszy = (int)(ListingFeeAmount * 100);

            var payload = new
            {
                notifyUrl = _notifyUrl,
                customerIp = "127.0.0.1",
                merchantPosId = _merchantPosId,
                description = $"Opłata za wystawienie: {listingTitle}",
                currencyCode = "PLN",
                totalAmount = amountGroszy.ToString(),
                recurring = "STANDARD",
                payMethods = new
                {
                    payMethod = new { type = "CARD", value = card.PayUCardToken }
                },
                products = new[]
                {
                    new { name = $"Wystawienie ogłoszenia: {listingTitle}", unitPrice = amountGroszy.ToString(), quantity = "1" }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_payUBaseUrl}/api/v2_1/orders", content, cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != global::System.Net.HttpStatusCode.Found)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Listing fee charge failed for seller {SellerId}: {Error}", sellerUserId, err);
                return (false, $"Płatność odrzucona przez PayU: {response.StatusCode}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Listing fee charge exception for seller {SellerId}", sellerUserId);
            return (false, ex.Message);
        }
    }

    public async Task TryDispatchPayoutAsync(Order order, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SellerPayouts
            .AnyAsync(p => p.OrderId == order.Id, cancellationToken);
        if (existing)
            return;

        var gross = order.Amount;
        var commission = Math.Round(gross * CommissionRate, 2);
        var net = gross - commission;

        var payout = new SellerPayout
        {
            SellerId = order.SellerId,
            OrderId = order.Id,
            GrossAmount = gross,
            CommissionAmount = commission,
            NetAmount = net,
            Status = SellerPayoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.SellerPayouts.Add(payout);
        await _context.SaveChangesAsync(cancellationToken);

        var card = await _context.SellerCards
            .FirstOrDefaultAsync(c => c.UserId == order.SellerId && c.IsActive, cancellationToken);

        if (card == null)
        {
            payout.Status = SellerPayoutStatus.NoCard;
            payout.ErrorMessage = "Sprzedawca nie ma podpiętej karty. Wypłata wstrzymana.";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var token = await GetPayUTokenAsync(cancellationToken);
            var client = CreatePayUClient(token);

            var amountGroszy = (int)Math.Round(net * 100, MidpointRounding.AwayFromZero);

            var payload = new
            {
                payout = new
                {
                    description = $"Wypłata za sprzedaż #{order.Id}",
                    amount = amountGroszy.ToString(),
                    currencyCode = "PLN",
                    payMethod = new { type = "CARD", value = card.PayUCardToken }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_payUBaseUrl}/api/v2_1/payouts", content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(json);
                string? payoutId = null;
                if (doc.RootElement.TryGetProperty("payout", out var p) && p.TryGetProperty("payoutId", out var pid))
                    payoutId = pid.GetString();

                payout.Status = SellerPayoutStatus.Paid;
                payout.PayUPayoutId = payoutId;
                payout.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                payout.Status = SellerPayoutStatus.Failed;
                payout.ErrorMessage = $"PayU odrzucił wypłatę ({response.StatusCode}): {json}";
                _logger.LogWarning("Payout failed for OrderId={OrderId} SellerId={SellerId}: {Error}",
                    order.Id, order.SellerId, payout.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            payout.Status = SellerPayoutStatus.Failed;
            payout.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Payout exception for OrderId={OrderId}", order.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GetPayUTokenAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });

        var response = await client.PostAsync($"{_payUBaseUrl}/pl/standard/user/oauth/authorize", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PayU OAuth error: {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("PayU: brak access_token.");
    }

    private HttpClient CreatePayUClient(string token)
    {
        var client = _httpClientFactory.CreateClient("PayU");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
