using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Review
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public Listing Listing { get; set; }
        public int Rating { get; set; }
        public int ReviewerId { get; set; }
        public User Reviewer { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Upvotes { get; set; } = 0;
        public int Downvotes { get; set; } = 0;

        public ICollection<ReviewPhoto> Photos { get; set; }
    }
}