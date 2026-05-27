namespace ProjektZespolowyGr3.Helpers
{
    public static class PriceHelper
    {
        public static string FormatPricePLN(this decimal? price)
            => price.HasValue
                ? $"{price.Value.ToString("N2", PolishPriceCulture)} PLN"
                : "Brak ceny";

        public static string FormatPricePLN(this decimal price)
            => price.ToString("N2", PolishPriceCulture) + " PLN";

        private static System.Globalization.CultureInfo PolishPriceCulture
        {
            get
            {
                var culture = new System.Globalization.CultureInfo("pl-PL");
                culture.NumberFormat.NumberGroupSeparator = " ";
                return culture;
            }
        }
    }
}
