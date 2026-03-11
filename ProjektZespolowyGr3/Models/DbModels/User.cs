namespace ProjektZespolowyGr3.Models.DbModels
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public string? Address { get; set; }
        public bool IsBanned { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Listing> Listings { get; set; }
        public ICollection<Review> Reviews { get; set; }

        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}
