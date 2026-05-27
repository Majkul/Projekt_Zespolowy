using System.Globalization;
using FluentAssertions;
using ProjektZespolowyGr3.Helpers;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Services
{
    public class PriceHelperTests
    {
        [Fact]
        public void FormatPricePLN_ShouldFormatNullableDecimalUsingPolishFormat()
        {
            decimal? price = 1234.5m;

            var result = price.FormatPricePLN();

            result.Should().Be("1 234,50 PLN");
        }

        [Fact]
        public void FormatPricePLN_ShouldReturnMissingPriceText_WhenNullableDecimalIsNull()
        {
            decimal? price = null;

            var result = price.FormatPricePLN();

            result.Should().Be("Brak ceny");
        }

        [Fact]
        public void FormatPricePLN_ShouldFormatDecimalUsingPolishFormatRegardlessOfCurrentCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                CultureInfo.CurrentUICulture = new CultureInfo("en-US");

                var result = 1234.5m.FormatPricePLN();

                result.Should().Be("1 234,50 PLN");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }
    }
}
