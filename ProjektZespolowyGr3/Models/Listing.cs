namespace ProjektZespolowyGr3.Models
{
    public class Listing
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int SellerId { get; set; }
        public User Seller { get; set; }
        public ListingType Type { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal? Price { get; set; }

    }
}

public enum ListingType
{
    Sale,
    Trade
}