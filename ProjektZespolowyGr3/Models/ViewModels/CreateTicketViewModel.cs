using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class CreateTicketViewModel
    {
        [Required, StringLength(120)]
        public string Subject { get; set; }

        [Required, StringLength(2000)]
        public string Description { get; set; }

        [Required]
        public TicketCategory Category { get; set; }

        public int? ReportedUserId { get; set; }
        public int? ReportedListingId { get; set; }
        public string? ReportedListingTitle { get; set; }
        public string? ReportedUserName { get; set; }

        public List<IFormFile>? Attachments { get; set; } = new();
    }
}
