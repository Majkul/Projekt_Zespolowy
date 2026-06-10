namespace ProjektZespolowyGr3.Models.DbModels
{
    public class TradeProposalItem
    {
        public int Id { get; set; }

        public int TradeProposalId { get; set; }
        public TradeProposal TradeProposal { get; set; } = null!;

        public TradeProposalSide Side { get; set; }

        public int? ListingId { get; set; }
        public Listing? Listing { get; set; }

        /// <summary>Liczba sztuk danego ogłoszenia w tej propozycji (tylko gdy <see cref="ListingId"/> jest ustawione).</summary>
        public int Quantity { get; set; } = 1;

        /// <summary>Dopłata gotówką po danej stronie wymiany.</summary>
        public decimal? CashAmount { get; set; }

        /// <summary>Tytuł/opis nieformalnego przedmiotu (niezgłoszonego w systemie) dodanego do oferty.</summary>
        public string? CustomOfferTitle { get; set; }

        /// <summary>Szacowana wartość nieformalnego przedmiotu (PLN).</summary>
        public decimal? CustomOfferEstimatedValue { get; set; }
    }
}
