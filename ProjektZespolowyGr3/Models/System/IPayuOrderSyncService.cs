namespace ProjektZespolowyGr3.Models.System;

public interface IPayuOrderSyncService
{
    Task HandleNotifyAsync(string payuOrderId, string? payuStatus,
        string? cardToken = null, string? cardMasked = null, string? cardBrand = null,
        int cardExpiryMonth = 0, int cardExpiryYear = 0,
        CancellationToken cancellationToken = default);

    Task TryFinalizeOrderFromPayuApiAsync(int orderId, CancellationToken cancellationToken = default);

    Task TryFinalizeTradeOrderFromPayuApiAsync(int tradeOrderId, CancellationToken cancellationToken = default);

    Task SyncPendingOrdersForSellerAsync(int sellerUserId, CancellationToken cancellationToken = default);

    Task EnsureListingPurchasedNotificationIfNeededAsync(int orderId, CancellationToken cancellationToken = default);
}
