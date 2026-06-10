using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Helpers
{
    public sealed record ListingBadge(string Label, string CssClass);

    public static class ListingPresentationHelper
    {
        public static IReadOnlyList<ListingBadge> GetListingBadges(Listing listing)
        {
            var badges = new List<ListingBadge>();

            if (listing.Price.HasValue)
            {
                badges.Add(new ListingBadge("Sprzedaż", "bg-primary-lt"));
            }

            if (!listing.NotExchangeable)
            {
                badges.Add(new ListingBadge("Wymiana", "bg-green-lt"));
            }

            return badges;
        }

        public static string FormatNewListingsCount(int count)
        {
            var noun = GetPolishPlural(count, "nowa oferta", "nowe oferty", "nowych ofert");
            return $"{count} {noun}";
        }

        public static string FormatSaleListingsCount(int count)
        {
            var noun = GetPolishPlural(count, "oferta sprzedaży", "oferty sprzedaży", "ofert sprzedaży");
            return $"{count} {noun}";
        }

        public static string FormatTradeListingsCount(int count)
        {
            var noun = GetPolishPlural(count, "oferta wymiany", "oferty wymiany", "ofert wymiany");
            return $"{count} {noun}";
        }

        private static string GetPolishPlural(int count, string singular, string few, string many)
        {
            var absoluteCount = Math.Abs(count);
            var lastDigit = absoluteCount % 10;
            var lastTwoDigits = absoluteCount % 100;

            if (absoluteCount == 1)
            {
                return singular;
            }

            if (lastDigit is >= 2 and <= 4 && lastTwoDigits is not (>= 12 and <= 14))
            {
                return few;
            }

            return many;
        }
    }
}
