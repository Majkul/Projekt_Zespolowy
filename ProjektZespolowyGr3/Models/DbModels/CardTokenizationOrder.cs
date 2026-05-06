namespace ProjektZespolowyGr3.Models.DbModels
{
    public class CardTokenizationOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string PayUOrderId { get; set; } = "";
        public bool Completed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
