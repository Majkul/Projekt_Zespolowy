using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class ComposeTradeViewModel
    {
        public int SubjectListingId { get; set; }
        public Listing SubjectListing { get; set; } = null!;

        /// <summary>Edycja istniejącej propozycji (tylko initiator, Pending).</summary>
        public int? EditTradeProposalId { get; set; }

        /// <summary>Kontroferta – nadrzędna propozycja.</summary>
        public int? ParentTradeProposalId { get; set; }

        public List<Listing> InitiatorPool { get; set; } = new();
        public List<Listing> ReceiverPool { get; set; } = new();

        public List<int> SelectedInitiatorListingIds { get; set; } = new();
        public List<int> SelectedReceiverListingIds { get; set; } = new();

        /// <summary>Liczba sztuk na ogłoszenie (inicjator) — klucz: ListingId.</summary>
        public Dictionary<int, int> InitiatorQuantities { get; set; } = new();

        /// <summary>Liczba sztuk na ogłoszenie (odbiorca) — klucz: ListingId.</summary>
        public Dictionary<int, int> ReceiverQuantities { get; set; } = new();

        public decimal InitiatorCash { get; set; }
        public decimal ReceiverCash { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>Login użytkownika składającego tę propozycję (lewa kolumna — co oferuje).</summary>
        public string InitiatorUsername { get; set; } = "";

        /// <summary>Login drugiej strony — odbiorca propozycji (prawa kolumna — czego oczekujesz od rozmówcy).</summary>
        public string ReceiverUsername { get; set; } = "";

        /// <summary>
        /// Kiedy true: kupujący w kontekście ogłoszenia ustawia ofertę w <strong>lewej</strong> (niebieskiej) kolumnie.
        /// Gdy false (np. kontroferta od sprzedawcy): kupujący to druga strona — suma z <strong>prawej</strong> kolumny.
        /// </summary>
        public bool BuyerOffersFromInitiatorColumn { get; set; } = true;

        /// <summary>Przy kontrofercie – propozycja, na którą odpowiadasz (tylko do podglądu).</summary>
        public TradeProposal? ParentProposal { get; set; }
    }
}
