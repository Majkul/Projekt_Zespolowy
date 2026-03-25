using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.System;

public static class ListingStockHelper
{
    public static void ApplySale(Listing listing, int quantity)
    {
        if (quantity <= 0 || listing.StockQuantity <= 0)
            return;
        var q = Math.Min(quantity, listing.StockQuantity);
        listing.StockQuantity -= q;
        SyncSoldFlag(listing);
    }

    public static bool CanSell(Listing listing, int quantity = 1) =>
        !listing.IsArchived
        && !listing.IsSold
        && listing.StockQuantity >= quantity
        && quantity > 0;

    public static bool IsAvailableForTrade(Listing listing) =>
        !listing.IsArchived && !listing.NotExchangeable && CanSell(listing, 1);

    public static void SyncSoldFlag(Listing listing)
    {
        if (listing.StockQuantity <= 0)
        {
            listing.StockQuantity = 0;
            listing.IsSold = true;
        }
        else
            listing.IsSold = false;
    }
}
