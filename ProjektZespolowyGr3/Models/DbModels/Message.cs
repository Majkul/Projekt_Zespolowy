using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }
        public User Sender { get; set; }

        [Required]
        public int ReceiverId { get; set; }
        public User Receiver { get; set; }

        public int? ListingId { get; set; }
        public Listing? Listing { get; set; }

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; } = false;
    }
}