namespace ProjektZespolowyGr3.Models.DbModels
{
    /// <summary>
    /// Tagi kategorii – jeśli ustawione, przedmioty złożone w wymianie muszą mieć co najmniej jeden z tych tagów.
    /// </summary>
    public class ListingExchangeAcceptedTag
    {
        public int ListingId { get; set; }
        public Listing Listing { get; set; } = null!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
