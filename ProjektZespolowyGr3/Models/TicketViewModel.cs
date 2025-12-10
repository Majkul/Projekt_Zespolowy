using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models
{
    public enum TicketStatus
    {
        Open,
        Pending,
        Closed,
        Rejected
    }

    public class TicketViewModel
    {
        public int Id { get; set; }

        [Required]
        public int OfferId { get; set; }  // Link to the suspicious offer

        [Required]
        public string ReportedByUserId { get; set; }  // Klient who reported

        [Required(ErrorMessage = "Powód zg³oszenia jest wymagany.")]
        [StringLength(1000, ErrorMessage = "Powód zg³oszenia mo¿e mieæ maksymalnie 1000 znaków.")]
        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public string AdminAction { get; set; }  // e.g. "Oferta usuniêta", "U¿ytkownik zablokowany", "Brak akcji"
    }
}

