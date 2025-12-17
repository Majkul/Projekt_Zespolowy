using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Listing
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }

        public int SellerId { get; set; }
        public User Seller { get; set; }
        public ListingType Type { get; set; }
        public decimal? Price { get; set; }
        public bool IsFeatured { get; set; } = false;
        public bool IsSold { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
        public ICollection<ListingTag> Tags { get; set; } = new List<ListingTag>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}

public enum ListingType
{
    Sale,
    Trade
}