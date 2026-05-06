namespace ProjektZespolowyGr3.Models.DbModels
{
    public enum SellerPayoutStatus
    {
        Pending,
        Paid,
        Failed,
        NoCard
    }

    public class SellerPayout
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public User Seller { get; set; } = null!;
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }
        public SellerPayoutStatus Status { get; set; }
        public string? PayUPayoutId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
