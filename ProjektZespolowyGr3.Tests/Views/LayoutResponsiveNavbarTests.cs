using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Views
{
    public class LayoutResponsiveNavbarTests
    {
        [Fact]
        public void Layout_ShouldUseSingleMobileNavbarCollapseTarget()
        {
            var layout = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Shared", "_Layout.cshtml"));

            layout.Should().Contain("navbar-expand-lg");
            layout.Should().Contain("data-bs-target=\"#navbarMain\"");
            layout.Should().Contain("aria-controls=\"navbarMain\"");
            layout.Should().Contain("id=\"navbarMain\"");
            layout.Should().NotContain("data-bs-target=\".navbar-collapse\"");
        }

        [Fact]
        public void Layout_ShouldRenderSearchAndUserActionsInsideCollapsibleNavbar()
        {
            var layout = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Shared", "_Layout.cshtml"));

            var collapseIndex = layout.IndexOf("id=\"navbarMain\"", StringComparison.Ordinal);
            var searchFormIndex = layout.IndexOf("<form class=\"d-flex w-100 my-2", StringComparison.Ordinal);
            var notificationsIndex = layout.IndexOf("UnreadNotifications", StringComparison.Ordinal);
            var messagesIndex = layout.IndexOf("asp-controller=\"Messages\"", StringComparison.Ordinal);
            var userDropdownIndex = layout.IndexOf("id=\"userDropdown\"", StringComparison.Ordinal);

            collapseIndex.Should().BeGreaterThan(-1);
            searchFormIndex.Should().BeGreaterThan(collapseIndex);
            notificationsIndex.Should().BeGreaterThan(collapseIndex);
            messagesIndex.Should().BeGreaterThan(collapseIndex);
            userDropdownIndex.Should().BeGreaterThan(collapseIndex);
            layout.Should().Contain("<ul class=\"navbar-nav ms-auto align-items-center\"");
        }

        [Fact]
        public void Layout_ShouldRenderNavbarSearchOnlyWhenNotHidden()
        {
            var layout = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Shared", "_Layout.cshtml"));

            var conditionIndex = layout.IndexOf("@if (ViewData[\"HideNavSearch\"] as bool? != true)", StringComparison.Ordinal);
            var searchFormIndex = layout.IndexOf("<form class=\"d-flex w-100 my-2", StringComparison.Ordinal);

            conditionIndex.Should().BeGreaterThan(-1);
            searchFormIndex.Should().BeGreaterThan(conditionIndex);
            layout.Should().NotContain("display:none");
        }

        [Fact]
        public void Layout_ShouldLoadTomSelectAssetsAfterBootstrap()
        {
            var layout = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "Views", "Shared", "_Layout.cshtml"));

            var bootstrapCssIndex = layout.IndexOf("~/lib/bootstrap/dist/css/bootstrap.min.css", StringComparison.Ordinal);
            var tomSelectCssIndex = layout.IndexOf("tom-select.bootstrap5.min.css", StringComparison.Ordinal);
            var bootstrapJsIndex = layout.IndexOf("~/lib/bootstrap/dist/js/bootstrap.bundle.min.js", StringComparison.Ordinal);
            var tomSelectJsIndex = layout.IndexOf("tom-select.complete.min.js", StringComparison.Ordinal);

            bootstrapCssIndex.Should().BeGreaterThan(-1);
            tomSelectCssIndex.Should().BeGreaterThan(bootstrapCssIndex);
            bootstrapJsIndex.Should().BeGreaterThan(-1);
            tomSelectJsIndex.Should().BeGreaterThan(bootstrapJsIndex);
        }

        [Fact]
        public void SiteCss_ShouldLimitExpandedNavbarHeightOnSmallScreens()
        {
            var css = ReadProjectFile(Path.Combine("ProjektZespolowyGr3", "wwwroot", "css", "site.css"));

            css.Should().Contain("@media (max-width: 991.98px)");
            css.Should().Contain(".navbar-collapse");
            css.Should().Contain("overflow-y: auto;");
            css.Should().Contain("max-height: 80vh;");
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
