using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.System;

public class PayuOrderSyncService : IPayuOrderSyncService
{
    private readonly MyDBContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notifications;
    private readonly ILogger<PayuOrderSyncService> _logger;

    private readonly string _payUBaseUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public PayuOrderSyncService(
        MyDBContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        INotificationService notifications,
        ILogger<PayuOrderSyncService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _notifications = notifications;
        _logger = logger;

        _payUBaseUrl = configuration["PayU:BaseUrl"] ?? "";
        _clientId = configuration["PayU:ClientId"] ?? "";
        _clientSecret = configuration["PayU:ClientSecret"] ?? "";
    }

    public async Task HandleNotifyAsync(string payuOrderId, string? payuStatus, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(payuOrderId))
            return;

        var completed = !string.IsNullOrEmpty(payuStatus) &&
            payuStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase);

        // Check regular Order first
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.PayUOrderId == payuOrderId, cancellationToken);
        if (order != null)
        {
            if (completed && order.Status != OrderStatus.Paid)
            {
                order.Status = OrderStatus.Paid;
                var listing = await _context.Listings.FindAsync(new object?[] { order.ListingId }, cancellationToken);
                if (listing != null)
                    ListingStockHelper.ApplySale(listing, Math.Max(1, order.Quantity));
                await _context.SaveChangesAsync(cancellationToken);
            }
            if (order.Status == OrderStatus.Paid)
                await EnsureListingPurchasedNotificationAsync(order, cancellationToken);
            return;
        }

        // Check TradeOrder
        var tradeOrder = await _context.TradeOrders
            .FirstOrDefaultAsync(o => o.PayUOrderId == payuOrderId, cancellationToken);
        if (tradeOrder != null && completed && tradeOrder.Status != OrderStatus.Paid)
        {
            tradeOrder.Status = OrderStatus.Paid;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task TryFinalizeOrderFromPayuApiAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order == null || order.Status != OrderStatus.Pending || string.IsNullOrEmpty(order.PayUOrderId))
            return;

        string? apiStatus;
        try
        {
            apiStatus = await FetchPayuOrderStatusAsync(order.PayUOrderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayU: nie udało się odczytać statusu dla OrderId={LocalOrderId}", orderId);
            return;
        }

        if (string.IsNullOrEmpty(apiStatus) || !apiStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
            return;

        order.Status = OrderStatus.Paid;
        var listing = await _context.Listings.FindAsync(new object?[] { order.ListingId }, cancellationToken);
        if (listing != null)
            ListingStockHelper.ApplySale(listing, Math.Max(1, order.Quantity));
        await _context.SaveChangesAsync(cancellationToken);

        await EnsureListingPurchasedNotificationAsync(order, cancellationToken);
    }

    public async Task TryFinalizeTradeOrderFromPayuApiAsync(int tradeOrderId, CancellationToken cancellationToken = default)
    {
        var tradeOrder = await _context.TradeOrders.FirstOrDefaultAsync(o => o.Id == tradeOrderId, cancellationToken);
        if (tradeOrder == null || tradeOrder.Status != OrderStatus.Pending || string.IsNullOrEmpty(tradeOrder.PayUOrderId))
            return;

        string? apiStatus;
        try
        {
            apiStatus = await FetchPayuOrderStatusAsync(tradeOrder.PayUOrderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayU: nie udało się odczytać statusu dla TradeOrderId={Id}", tradeOrderId);
            return;
        }

        if (string.IsNullOrEmpty(apiStatus) || !apiStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
            return;

        tradeOrder.Status = OrderStatus.Paid;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncPendingOrdersForSellerAsync(int sellerUserId, CancellationToken cancellationToken = default)
    {
        var ids = await _context.Orders.AsNoTracking()
            .Where(o => o.SellerId == sellerUserId
                        && o.Status == OrderStatus.Pending
                        && !string.IsNullOrEmpty(o.PayUOrderId))
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            try
            {
                await TryFinalizeOrderFromPayuApiAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sync pending PayU order LocalId={OrderId} dla SellerId={SellerId}", id, sellerUserId);
            }
        }

        var paidMissingNotify = await _context.Orders.AsNoTracking()
            .Where(o => o.SellerId == sellerUserId && o.Status == OrderStatus.Paid)
            .Where(o => !_context.Notifications.Any(n =>
                n.OrderId == o.Id && n.UserId == sellerUserId && n.Kind == NotificationKind.ListingPurchased))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => o.Id)
            .Take(30)
            .ToListAsync(cancellationToken);

        foreach (var oid in paidMissingNotify)
            await EnsureListingPurchasedNotificationIfNeededAsync(oid, cancellationToken);
    }

    public async Task EnsureListingPurchasedNotificationIfNeededAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order != null)
            await EnsureListingPurchasedNotificationAsync(order, cancellationToken);
    }

    private async Task EnsureListingPurchasedNotificationAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.Status != OrderStatus.Paid)
            return;

        var exists = await _context.Notifications.AnyAsync(n =>
            n.OrderId == order.Id &&
            n.UserId == order.SellerId &&
            n.Kind == NotificationKind.ListingPurchased, cancellationToken);
        if (exists)
            return;

        await _notifications.NotifyListingPurchasedAsync(order.SellerId, order.Id, cancellationToken);
    }

    private async Task<string?> FetchPayuOrderStatusAsync(string payuOrderId, CancellationToken cancellationToken)
    {
        var token = await GetPayUTokenAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient("PayU");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var id = Uri.EscapeDataString(payuOrderId);
        var response = await client.GetAsync($"{_payUBaseUrl}/api/v2_1/orders/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayU GET order {PayuOrderId}: {Status}", payuOrderId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("orders", out var orders) ||
            orders.ValueKind != JsonValueKind.Array ||
            orders.GetArrayLength() == 0)
            return TryParseStatusAlternative(doc.RootElement);

        var first = orders[0];
        if (first.TryGetProperty("status", out var st))
            return st.GetString();

        return TryParseStatusAlternative(first);
    }

    private static string? TryParseStatusAlternative(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("status", out var st))
        {
            if (st.ValueKind == JsonValueKind.String)
                return st.GetString();
            if (st.ValueKind == JsonValueKind.Object && st.TryGetProperty("statusCode", out var code))
                return code.GetString();
        }

        return null;
    }

    private async Task<string> GetPayUTokenAsync(CancellationToken cancellationToken)
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
            content,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PayU OAuth error: {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("PayU: brak access_token");
    }
}
