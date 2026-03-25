using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.System
{
    public class NotificationService : INotificationService
    {
        private readonly MyDBContext _context;

        public NotificationService(MyDBContext context)
        {
            _context = context;
        }

        public async Task NotifyNewMessageAsync(int receiverUserId, int messageId, CancellationToken cancellationToken = default)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == receiverUserId, cancellationToken))
                return;

            _context.Notifications.Add(new Notification
            {
                UserId = receiverUserId,
                Kind = NotificationKind.NewMessage,
                MessageId = messageId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task NotifyListingPurchasedAsync(int sellerUserId, int orderId, CancellationToken cancellationToken = default)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == sellerUserId, cancellationToken))
                return;

            _context.Notifications.Add(new Notification
            {
                UserId = sellerUserId,
                Kind = NotificationKind.ListingPurchased,
                OrderId = orderId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task NotifyTradeProposalAsync(int receiverUserId, int tradeProposalId, CancellationToken cancellationToken = default)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == receiverUserId, cancellationToken))
                return;

            _context.Notifications.Add(new Notification
            {
                UserId = receiverUserId,
                Kind = NotificationKind.TradeProposalReceived,
                TradeProposalId = tradeProposalId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
