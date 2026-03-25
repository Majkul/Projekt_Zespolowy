namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Order
    {
        public int Id { get; set; }

        public int ListingId { get; set; }
        public Listing? Listing { get; set; }

        public int BuyerId { get; set; }
        public int SellerId { get; set; }

        public decimal Amount { get; set; }

        /// <summary>Liczba kupionych sztuk w tym zamówieniu.</summary>
        public int Quantity { get; set; } = 1;

        public string PayUOrderId { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsArchived { get; set; } = false;
    }

    public enum OrderStatus
    {
        Pending,
        Paid,
        Cancelled
    }

}
