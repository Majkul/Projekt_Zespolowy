using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class TradeProposalHistoryEntry
    {
        public int Id { get; set; }

        public int TradeProposalId { get; set; }
        public TradeProposal TradeProposal { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime ChangedAt { get; set; }

        [MaxLength(500)]
        public string Summary { get; set; } = "";
    }
}
