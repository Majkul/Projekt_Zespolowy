namespace ProjektZespolowyGr3.Models.ViewModels
{
	public class PaymentViewModel
	{
		public int OrderId { get; set; }
		public string ListingTitle { get; set; }
		public decimal Amount { get; set; }
		public string MerchantPosId { get; set; }
	}
}
