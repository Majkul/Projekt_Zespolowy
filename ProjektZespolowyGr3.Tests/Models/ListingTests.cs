using Xunit;
using FluentAssertions;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Tests.Models
{
    public class ListingTests
    {
        [Fact]
        public void Listing_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var listing = new Listing
            {
                Id = 1,
                Title = "Test Listing",
                Description = "Test Description",
                SellerId = 1,
                Price = 100.50m,
                IsFeatured = false,
                IsSold = false,
                StockQuantity = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Assert
            listing.Id.Should().Be(1);
            listing.Title.Should().Be("Test Listing");
            listing.Description.Should().Be("Test Description");
            listing.SellerId.Should().Be(1);
            listing.Price.Should().Be(100.50m);
            listing.IsFeatured.Should().BeFalse();
            listing.IsSold.Should().BeFalse();
            listing.StockQuantity.Should().Be(1);
        }

        [Fact]
        public void Listing_ShouldSupportTradeType_WithoutPrice()
        {
            // Arrange & Act
            var listing = new Listing
            {
                Title = "Trade Listing",
                Description = "Want to trade",
                SellerId = 1,
                Price = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Assert
            listing.Price.Should().BeNull();
        }

        [Fact]
        public void Listing_ShouldSupportOptionalDescription()
        {
            // Arrange & Act
            var listing = new Listing
            {
                Title = "Listing without description",
                Description = null,
                SellerId = 1,
                Price = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Assert
            listing.Description.Should().BeNull();
        }
    }
}

