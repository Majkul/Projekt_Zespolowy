using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class CreateReviewViewModel
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } = 5;
        public int ListingId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public List<IFormFile>? PhotoFiles { get; set; } = new();
    }
}
