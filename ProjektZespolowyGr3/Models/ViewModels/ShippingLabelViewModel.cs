namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class ShippingParty
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class ShippingLabelViewModel
    {
        public ShippingParty From { get; set; } = new();
        public ShippingParty To { get; set; } = new();
        public string Contents { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string LabelType { get; set; } = string.Empty;
    }

    public class TradeLabelViewModel
    {
        public ShippingLabelViewModel LabelA { get; set; } = new();
        public ShippingLabelViewModel LabelB { get; set; } = new();
        public int TradeProposalId { get; set; }
    }
}
