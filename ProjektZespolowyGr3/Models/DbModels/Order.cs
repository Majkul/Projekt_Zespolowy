namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Order
    {
        public int Id { get; set; }

        public int ListingId { get; set; }
        public int BuyerId { get; set; }
        public int SellerId { get; set; }

        public decimal Amount { get; set; }

        public string PayUOrderId { get; set; }
        public OrderStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Paid,
        Cancelled
    }

}
