using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Listing
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int SellerId { get; set; }
        public User Seller { get; set; } = null!;
        public ListingType Type { get; set; }
        public decimal? Price { get; set; }

        /// <summary>Liczba dostępnych sztuk (ogłoszenie widoczne jako do kupna/wymiany dopóki &gt; 0).</summary>
        public int StockQuantity { get; set; } = 1;

        public bool IsFeatured { get; set; } = false;
        public bool IsSold { get; set; } = false;
        public bool IsArchived { get; set; } = false;
<<<<<<< HEAD
        public DateTime? ArchivedAt { get; set; }
=======

        /// <summary>Jeśli true, ogłoszenie nie może być dodane do propozycji wymiany.</summary>
        public bool NotExchangeable { get; set; }

        /// <summary>Minimalna suma szacowanej wartości po stronie oferującego (kupującego), aby wymiana była dozwolona.</summary>
        public decimal? MinExchangeValue { get; set; }

        /// <summary>Co sprzedający przyjąłby w zamian (opis oczekiwań przy wymianie).</summary>
        public string? ExchangeDescription { get; set; }

>>>>>>> origin/main
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
        public ICollection<ListingTag> Tags { get; set; } = new List<ListingTag>();
        public ICollection<ListingExchangeAcceptedTag> ExchangeAcceptedTags { get; set; } = new List<ListingExchangeAcceptedTag>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<TradeProposal> TradeProposalsAsSubject { get; set; } = new List<TradeProposal>();
    }
}

public enum ListingType
{
    Sale,
    Trade
}