using FluentAssertions;
using ProjektZespolowyGr3.Helpers;
using ProjektZespolowyGr3.Models.DbModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Services
{
    public class ListingPresentationHelperTests
    {
        [Theory]
        [InlineData(1, "1 nowa oferta")]
        [InlineData(2, "2 nowe oferty")]
        [InlineData(5, "5 nowych ofert")]
        public void FormatNewListingsCount_ShouldUsePolishDeclension(int count, string expected)
        {
            ListingPresentationHelper.FormatNewListingsCount(count).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, "1 oferta sprzedaży")]
        [InlineData(2, "2 oferty sprzedaży")]
        [InlineData(5, "5 ofert sprzedaży")]
        public void FormatSaleListingsCount_ShouldUsePolishDeclension(int count, string expected)
        {
            ListingPresentationHelper.FormatSaleListingsCount(count).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, "1 oferta wymiany")]
        [InlineData(2, "2 oferty wymiany")]
        [InlineData(5, "5 ofert wymiany")]
        public void FormatTradeListingsCount_ShouldUsePolishDeclension(int count, string expected)
        {
            ListingPresentationHelper.FormatTradeListingsCount(count).Should().Be(expected);
        }

        [Fact]
        public void GetListingBadges_ShouldReturnSaleAndTrade_WhenListingHasPriceAndAllowsExchange()
        {
            var listing = new Listing
            {
                Price = 100m,
                NotExchangeable = false
            };

            var result = ListingPresentationHelper.GetListingBadges(listing);

            result.Should().BeEquivalentTo(new[]
            {
                new ListingBadge("Sprzedaż", "bg-primary-lt"),
                new ListingBadge("Wymiana", "bg-green-lt")
            }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetListingBadges_ShouldAddNewBadge_WhenListingIsSevenDaysOldOrNewer()
        {
            var now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var listing = new Listing
            {
                CreatedAt = now.AddDays(-7),
                Price = 100m,
                NotExchangeable = true
            };

            var result = ListingPresentationHelper.GetListingBadges(listing, now);

            result.Should().BeEquivalentTo(new[]
            {
                new ListingBadge("Nowe", "bg-yellow-lt"),
                new ListingBadge("Sprzedaż", "bg-primary-lt")
            }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetListingBadges_ShouldNotAddNewBadge_WhenListingIsOlderThanSevenDays()
        {
            var now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var listing = new Listing
            {
                CreatedAt = now.AddDays(-7).AddTicks(-1),
                Price = 100m,
                NotExchangeable = true
            };

            var result = ListingPresentationHelper.GetListingBadges(listing, now);

            result.Should().BeEquivalentTo(new[]
            {
                new ListingBadge("Sprzedaż", "bg-primary-lt")
            }, options => options.WithStrictOrdering());
        }
    }
}
