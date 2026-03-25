namespace ProjektZespolowyGr3.Models.System;

public interface IPayuOrderSyncService
{
    Task HandleNotifyAsync(string payuOrderId, string? payuStatus, CancellationToken cancellationToken = default);

    Task TryFinalizeOrderFromPayuApiAsync(int orderId, CancellationToken cancellationToken = default);

    Task SyncPendingOrdersForSellerAsync(int sellerUserId, CancellationToken cancellationToken = default);

    Task EnsureListingPurchasedNotificationIfNeededAsync(int orderId, CancellationToken cancellationToken = default);
}
