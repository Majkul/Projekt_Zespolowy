namespace ProjektZespolowyGr3.Models.DbModels
{
    public class SellerCard
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string PayUCardToken { get; set; } = "";
        public string MaskedNumber { get; set; } = "";
        public string Brand { get; set; } = "";
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
