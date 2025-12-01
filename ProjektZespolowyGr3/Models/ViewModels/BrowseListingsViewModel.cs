using Microsoft.AspNetCore.Mvc.Rendering;
using ProjektZespolowyGr3.Models.DbModels;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class BrowseListingsViewModel
    {
        public Listing Listing { get; set; }
        public int ListingId { get; set; }
        public User Seller { get; set; }
        public int SellerId { get; set; }

        public string PhotoUrl { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string SellerName { get; set; }
    }
}
