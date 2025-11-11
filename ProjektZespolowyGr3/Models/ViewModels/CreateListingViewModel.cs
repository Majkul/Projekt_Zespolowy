using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class CreateListingViewModel
    {
        [Required, StringLength(120)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public ListingType Type { get; set; }

        public decimal? Price { get; set; }

        public List<IFormFile> PhotoFiles { get; set; } = new();

        public List<int>? SelectedTagIds { get; set; } = new();
        public IEnumerable<SelectListItem> AvailableTags { get; set; } = new List<SelectListItem>();
    }
}
