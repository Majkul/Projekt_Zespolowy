using Microsoft.AspNetCore.Mvc.Rendering;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class ListingsFilterViewModel
    {
        public string? SearchString { get; set; }
        public string? Location { get; set; }
        public string? Type { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<int> SelectedTagIds { get; set; } = new();
        public string? SortBy { get; set; }

        public List<Tag> AvailableTags { get; set; } = new();
        public IEnumerable<BrowseListingsViewModel> FeaturedResults { get; set; } = new List<BrowseListingsViewModel>();
        public IEnumerable<BrowseListingsViewModel> Results { get; set; } = new List<BrowseListingsViewModel>();

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchString) ||
            !string.IsNullOrWhiteSpace(Location) ||
            !string.IsNullOrWhiteSpace(Type) ||
            MinPrice.HasValue ||
            MaxPrice.HasValue ||
            SelectedTagIds.Any();
    }
}
