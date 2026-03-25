namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public NotificationKind Kind { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        public int? MessageId { get; set; }
        public Message? Message { get; set; }

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        public int? TradeProposalId { get; set; }
        public TradeProposal? TradeProposal { get; set; }
    }

    public enum NotificationKind
    {
        NewMessage = 0,
        ListingPurchased = 1,
        TradeProposalReceived = 2
    }
}
