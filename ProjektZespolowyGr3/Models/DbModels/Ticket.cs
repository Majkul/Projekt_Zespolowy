using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Ticket
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public TicketCategory Category { get; set; }
        public TicketStatus Status { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }

        public int? AssigneeId { get; set; }
        public User? Assignee { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }

        public int? ReportedUserId { get; set; }
        public User? ReportedUser { get; set; }

        public int? ReportedListingId { get; set; }
        public Listing? ReportedListing { get; set; }

        public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    }
}

public enum TicketStatus
{
    Open,
    In_Progress,
    Resolved,
    Closed
}

public enum TicketCategory
{
    Payment_Issue,
    Order_Issue,
    Dispute,
    User_Report,
    Listing_Report,
    Technical_Issue,
    Other_Issue
}

