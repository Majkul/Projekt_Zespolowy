using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class CreateListingViewModel
    {
        [Required, StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Lokalizacja (miasto)")]
        public string? Location { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public decimal? Price { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "Ilość musi być między 1 a 1 000 000.")]
        [Display(Name = "Liczba sztuk")]
        public int StockQuantity { get; set; } = 1;

        public List<IFormFile> PhotoFiles { get; set; } = new();

        public List<int>? SelectedTagIds { get; set; } = new();
        public IEnumerable<SelectListItem> AvailableTags { get; set; } = new List<SelectListItem>();

        /// <summary>Opcje dostawy dodane przez sprzedającego.</summary>
        public List<ShippingOptionInput> ShippingOptions { get; set; } = new();

        public bool IsFeatured { get; set; }
        /// <summary>Nie uwzględniaj tego ogłoszenia w propozycjach wymiany.</summary>
        public bool NotExchangeable { get; set; } = true;

        public decimal? MinExchangeValue { get; set; }

        [StringLength(2000)]
        public string? ExchangeDescription { get; set; }

        /// <summary>Tagi kategorii akceptowane przy wymianie (puste = dowolne).</summary>
        public List<int>? SelectedExchangeAcceptedTagIds { get; set; } = new();
    }
}
