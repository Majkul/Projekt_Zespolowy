namespace ProjektZespolowyGr3.Models.DbModels
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsBanned { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public string? PhoneNumber { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

        public ICollection<TradeProposal> TradeProposalsAsInitiator { get; set; } = new List<TradeProposal>();
        public ICollection<TradeProposal> TradeProposalsAsReceiver { get; set; } = new List<TradeProposal>();
        public ICollection<TradeProposalHistoryEntry> TradeProposalHistoryEntries { get; set; } = new List<TradeProposalHistoryEntry>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
