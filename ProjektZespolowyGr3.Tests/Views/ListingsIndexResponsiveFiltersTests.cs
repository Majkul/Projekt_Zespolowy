using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Views
{
    public class ListingsIndexResponsiveFiltersTests
    {
        [Fact]
        public void Index_ShouldRenderMobileOffcanvasAndDesktopSidebarUsingSharedFilterPartial()
        {
            var index = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Listings", "Index.cshtml"));
            var partial = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Listings", "_FilterForm.cshtml"));

            index.Should().Contain("id=\"filterOffcanvas\"");
            index.Should().Contain("class=\"offcanvas offcanvas-start\"");
            index.Should().Contain("data-bs-toggle=\"offcanvas\"");
            index.Should().Contain("data-bs-target=\"#filterOffcanvas\"");
            index.Should().Contain("Filtruj ogłoszenia");
            index.Should().Contain("d-lg-none");
            index.Should().Contain("col-lg-3 d-none d-lg-block");
            index.Should().Contain("tagSelectFilterMobile");
            index.Should().Contain("tagSelectFilterDesktop");
            index.Split("@await Html.PartialAsync(\"_FilterForm\", Model,").Length.Should().Be(3);

            partial.Should().Contain("<form method=\"get\"");
            partial.Should().Contain("name=\"listingType\"");
            partial.Should().Contain("name=\"minPrice\"");
            partial.Should().Contain("name=\"maxPrice\"");
            partial.Should().Contain("name=\"tagIds\"");
            partial.Should().Contain("name=\"sortBy\"");
            partial.Should().Contain("name=\"maxDistanceKm\"");
            partial.Should().Contain("name=\"pageSize\"");
            partial.Should().Contain("Zastosuj filtry");
        }

        [Fact]
        public void Index_ShouldRenderPaginationLinksThatPreserveFilters()
        {
            var index = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Listings", "Index.cshtml"));

            index.Should().Contain("string PageUrl(int pageNumber)");
            index.Should().Contain("href=\"@PageUrl(pageNumber)\"");
            index.Should().Contain("new(\"page\"");
            index.Should().Contain("new(\"pageSize\"");
            index.Should().Contain("AddQueryValue(query, \"searchString\", searchString)");
            index.Should().Contain("new KeyValuePair<string, string?>(\"tagIds\"");
            index.Should().Contain("AddQueryValue(query, \"minPrice\", minPrice)");
            index.Should().Contain("AddQueryValue(query, \"maxPrice\", maxPrice)");
            index.Should().Contain("AddQueryValue(query, \"listingType\", listingType)");
            index.Should().Contain("AddQueryValue(query, \"sortBy\", sortBy)");
            index.Should().Contain("AddQueryValue(query, \"maxDistanceKm\", maxDistanceKm)");
            index.Should().Contain("AddQueryValue(query, \"userLat\", userLat)");
            index.Should().Contain("AddQueryValue(query, \"userLng\", userLng)");
        }

        [Fact]
        public void FilterForm_ShouldStartWithNeutralLocationPrompt()
        {
            var partial = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Listings", "_FilterForm.cshtml"));

            partial.Should().Contain("Użyj mojej lokalizacji");
            partial.Should().Contain("location-feedback");
            partial.Should().Contain("Najpierw użyj swojej lokalizacji");
            partial.Should().NotContain("maxDistanceKm.HasValue && (!userLat.HasValue || !userLng.HasValue)");
        }

        [Fact]
        public void ListingViews_ShouldUseTomSelectTagControls()
        {
            var create = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Listings", "Create.cshtml"));
            var partial = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Listings", "_FilterForm.cshtml"));
            var siteJs = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "wwwroot", "js", "site.js"));

            create.Should().Contain("id=\"tagSelectCreate\"");
            create.Should().Contain("asp-for=\"SelectedTagIds\"");
            create.Should().NotContain("Przytrzymaj Ctrl");

            partial.Should().Contain("var tagSelectId = ViewData[\"TagSelectId\"] as string ?? \"tagSelectFilter\";");
            partial.Should().Contain("id=\"@tagSelectId\"");
            partial.Should().Contain("multiple");
            partial.Should().Contain("name=\"tagIds\"");
            partial.Should().Contain("selectedTags.Contains(tag.Id)");
            partial.Should().NotContain("type=\"checkbox\"");

            var edit = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "ListingManage", "EditListing.cshtml"));
            edit.Should().Contain("id=\"tagSelectEdit\"");
            edit.Should().Contain("id=\"tagSelectEditExchange\"");

            siteJs.Should().Contain("document.addEventListener('DOMContentLoaded'");
            siteJs.Should().Contain("if (!window.TomSelect)");
            siteJs.Should().Contain("select[id^=\"tagSelect\"]");
            siteJs.Should().Contain("new TomSelect");
            siteJs.Should().Contain("plugins: ['remove_button']");
            siteJs.Should().Contain("placeholder: 'Wybierz tagi...'");
        }

        [Fact]
        public void FilterCard_ShouldKeepTomSelectDropdownAboveFollowingFields()
        {
            var siteCss = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "wwwroot", "css", "site.css"))
                .Replace("\r\n", "\n");

            siteCss.Should().Contain(".filter-card .ts-wrapper {\n    position: relative;\n}");
            siteCss.Should().Contain(".filter-card .ts-wrapper.dropdown-active {\n    z-index: 1055;\n}");
            siteCss.Should().Contain(".filter-card .ts-dropdown {\n    z-index: 1056;\n    background: #fff;");
        }

        private static string ReadProjectFile(string relativePath, [CallerFilePath] string sourceFilePath = "")
        {
            var directory = new FileInfo(sourceFilePath).Directory;

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);

                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not find project file: {relativePath}");
        }
    }
}
