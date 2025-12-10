using Microsoft.AspNetCore.Mvc.Rendering;
using ProjektZespolowyGr3.Models.DbModels;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class EditListingViewModel
    {
        [Required, StringLength(120)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public ListingType Type { get; set; }

        public decimal? Price { get; set; }

        public List<IFormFile> PhotoFiles { get; set; } = new();

        public List<int>? PhotosToDelete { get; set; } = new();

        public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
        public List<int>? SelectedTagIds { get; set; } = new();
        public IEnumerable<SelectListItem> AvailableTags { get; set; } = new List<SelectListItem>();
    }

}
