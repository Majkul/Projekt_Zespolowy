namespace ProjektZespolowyGr3.Models.System
{
    public interface INotificationService
    {
        Task NotifyNewMessageAsync(int receiverUserId, int messageId, CancellationToken cancellationToken = default);
        Task NotifyListingPurchasedAsync(int sellerUserId, int orderId, CancellationToken cancellationToken = default);
        Task NotifyTradeProposalAsync(int receiverUserId, int tradeProposalId, CancellationToken cancellationToken = default);
    }
}
