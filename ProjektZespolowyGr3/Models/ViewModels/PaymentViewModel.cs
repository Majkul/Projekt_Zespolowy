namespace ProjektZespolowyGr3.Models.ViewModels
{
	public class PaymentViewModel
	{
		public int OrderId { get; set; }
		public string ListingTitle { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public string MerchantPosId { get; set; } = string.Empty;
	}
}
