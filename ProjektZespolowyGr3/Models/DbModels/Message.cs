using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }
        public User? Sender { get; set; }

        [Required]
        public int ReceiverId { get; set; }
        public User? Receiver { get; set; }

        public int? ListingId { get; set; }
        public Listing? Listing { get; set; }

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int? TradeProposalId { get; set; }
        public TradeProposal? TradeProposal { get; set; }

        /// <summary>Odpowiedź na inną wiadomość (np. wątek kontrofert).</summary>
        public int? ReplyToMessageId { get; set; }
        public Message? ReplyToMessage { get; set; }
        public ICollection<MessagePhoto> Photos { get; set; } = new List<MessagePhoto>();
        public ICollection<Message> Replies { get; set; } = new List<Message>();

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; } = false;
        public bool IsArchived { get; set; } = false;
    }
}