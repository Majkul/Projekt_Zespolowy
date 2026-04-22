using FluentAssertions;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Models;

public class ListingStockHelperTests
{
    private static Listing Listing(int stock = 1, bool archived = false, bool sold = false) =>
        new()
        {
            Title = "x",
            SellerId = 1,
            Price = 10,
            StockQuantity = stock,
            IsArchived = archived,
            IsSold = sold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, -1, false)]
    [InlineData(1, 2, false)]
    public void CanSell_RespectsStockAndFlags(int stock, int qty, bool expected)
    {
        var l = Listing(stock: stock);
        ListingStockHelper.CanSell(l, qty).Should().Be(expected);
    }

    [Fact]
    public void CanSell_FalseWhenArchivedOrSold()
    {
        ListingStockHelper.CanSell(Listing(5, archived: true), 1).Should().BeFalse();
        ListingStockHelper.CanSell(Listing(5, sold: true), 1).Should().BeFalse();
    }

    [Fact]
    public void ApplySale_ReducesStockAndSetsSoldWhenEmpty()
    {
        var l = Listing(stock: 3);
        ListingStockHelper.ApplySale(l, 2);
        l.StockQuantity.Should().Be(1);
        l.IsSold.Should().BeFalse();

        ListingStockHelper.ApplySale(l, 5);
        l.StockQuantity.Should().Be(0);
        l.IsSold.Should().BeTrue();
    }

    [Fact]
    public void SyncSoldFlag_ClearsSoldWhenStockPositive()
    {
        var l = Listing(stock: 0);
        l.IsSold = true;
        l.StockQuantity = 2;
        ListingStockHelper.SyncSoldFlag(l);
        l.IsSold.Should().BeFalse();
    }

    [Fact]
    public void IsAvailableForTrade_RequiresSellableAndNotBlocked()
    {
        var ok = Listing(stock: 1);
        ok.NotExchangeable = false;
        ListingStockHelper.IsAvailableForTrade(ok).Should().BeTrue();

        ok.NotExchangeable = true;
        ListingStockHelper.IsAvailableForTrade(ok).Should().BeFalse();
    }
}
