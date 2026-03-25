using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Ticket
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public TicketCategory Category { get; set; }
        public TicketStatus Status { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? AssigneeId { get; set; }
        public User? Assignee { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsArchived { get; set; } = false;

        public int? ReportedUserId { get; set; }
        public User? ReportedUser { get; set; }

        public int? ReportedListingId { get; set; }
        public Listing? ReportedListing { get; set; }

        public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    }
}

public enum TicketStatus
{
    [Display(Name = "Otwarte")]
    Open,
    [Display(Name = "W trakcie")]
    In_Progress,
    [Display(Name = "Rozwiązane")]
    Resolved,
    [Display(Name = "Zamknięte")]
    Closed
}

public enum TicketCategory
{
    [Display(Name = "Problem z płatnością")]
    Payment_Issue,

    [Display(Name = "Problem z zamówieniem")]
    Order_Issue,

    [Display(Name = "Spór między użytkownikami")]
    Dispute,

    [Display(Name = "Zgłoszenie użytkownika")]
    User_Report,

    [Display(Name = "Zgłoszenie oferty")]
    Listing_Report,

    [Display(Name = "Problem techniczny")]
    Technical_Issue,

    [Display(Name = "Inny problem")]
    Other_Issue
}

