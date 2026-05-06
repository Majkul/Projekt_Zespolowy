using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    /// <summary>Płatność wykonana przez jedną ze stron wymiany (dopłata + koszt wysyłki).</summary>
    public class TradeOrder
    {
        public int Id { get; set; }

        public int TradeProposalId { get; set; }
        public TradeProposal TradeProposal { get; set; } = null!;

        /// <summary>UserId osoby płacącej.</summary>
        public int PayerUserId { get; set; }

        /// <summary>UserId osoby otrzymującej wartość (druga strona wymiany).</summary>
        public int ReceiverUserId { get; set; }

        /// <summary>Strona wymiany płatnika (Initiator / Receiver).</summary>
        public TradeProposalSide PayerSide { get; set; }

        /// <summary>Kwota dopłaty gotówkowej wynikająca z propozycji (może być 0).</summary>
        public decimal CashAmount { get; set; } = 0;

        /// <summary>Koszt wysyłki wybrany przez płatnika (może być 0).</summary>
        public decimal ShippingCost { get; set; } = 0;

        /// <summary>Łączna kwota do zapłaty (CashAmount + ShippingCost).</summary>
        public decimal TotalAmount { get; set; } = 0;

        /// <summary>Nazwa wybranej metody wysyłki.</summary>
        [StringLength(100)]
        public string? SelectedShippingName { get; set; }

        public string PayUOrderId { get; set; } = string.Empty;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
