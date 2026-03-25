using Microsoft.AspNetCore.Mvc.Rendering;
using ProjektZespolowyGr3.Models.DbModels;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class EditListingViewModel
    {
        [Required, StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public ListingType Type { get; set; }

        public decimal? Price { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "Ilość musi być między 0 a 1 000 000.")]
        [Display(Name = "Liczba sztuk")]
        public int StockQuantity { get; set; } = 1;

        public List<IFormFile> PhotoFiles { get; set; } = new();

        public List<int>? PhotosToDelete { get; set; } = new();

        public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
        public List<int>? SelectedTagIds { get; set; } = new();
        public IEnumerable<SelectListItem> AvailableTags { get; set; } = new List<SelectListItem>();

        public bool NotExchangeable { get; set; }

        public decimal? MinExchangeValue { get; set; }

        [StringLength(2000)]
        public string? ExchangeDescription { get; set; }

        public List<int>? SelectedExchangeAcceptedTagIds { get; set; } = new();
    }

}
