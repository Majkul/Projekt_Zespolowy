using System;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models
{
    public class MessageViewModel
    {
        public int Id { get; set; }

        [Required]
        public int ThreadId { get; set; } // Conversation thread ID

        [Required]
        public string SenderUserId { get; set; } // Must be logged-in user

        [Required]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Wiadomość musi mieć od 1 do 1000 znaków.")]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsBlocked { get; set; } = false; // If thread is blocked by offer author
    }

    public class ThreadViewModel
    {
        public int Id { get; set; }
        public string User1Id { get; set; }
        public string User2Id { get; set; }
        public bool IsBlocked { get; set; } = false;
    }
}

