using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class TradeProposal
    {
        public int Id { get; set; }

        public int InitiatorUserId { get; set; }
        public User Initiator { get; set; } = null!;

        public int ReceiverUserId { get; set; }
        public User Receiver { get; set; } = null!;

        /// <summary>Ogłoszenie, w kontekście którego złożono propozycję (po stronie odbiorcy).</summary>
        public int SubjectListingId { get; set; }
        public Listing SubjectListing { get; set; } = null!;

        public TradeProposalStatus Status { get; set; }

        public int? ParentTradeProposalId { get; set; }
        public TradeProposal? ParentTradeProposal { get; set; }

        public ICollection<TradeProposal> CounterOffers { get; set; } = new List<TradeProposal>();

        /// <summary>Pierwsza propozycja w wątku (dla podglądu łańcucha kontrofert).</summary>
        public int? RootTradeProposalId { get; set; }
        public TradeProposal? RootTradeProposal { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }

        public ICollection<TradeProposalItem> Items { get; set; } = new List<TradeProposalItem>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<TradeProposalHistoryEntry> History { get; set; } = new List<TradeProposalHistoryEntry>();
    }

    public enum TradeProposalStatus
    {
        [Display(Name = "Oczekuje")]
        Pending = 0,
        [Display(Name = "Zaakceptowana")]
        Accepted = 1,
        [Display(Name = "Odrzucona")]
        Rejected = 2,
        [Display(Name = "Anulowana")]
        Cancelled = 3,
        [Display(Name = "Zastąpiona kontrofertą")]
        Superseded = 4
    }

    public enum TradeProposalSide
    {
        Initiator = 0,
        Receiver = 1
    }
}
