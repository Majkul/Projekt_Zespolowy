using FluentAssertions;
using ProjektZespolowyGr3.Helpers;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Services
{
    public class SlugHelperTests
    {
        [Theory]
        [InlineData("Zażółć gęślą jaźń!", "zazolc-gesla-jazn")]
        [InlineData("Problem z płatnością #12", "problem-z-platnoscia-12")]
        [InlineData("  Stary   rower---Trek!!!  ", "stary-rower-trek")]
        public void GenerateSlug_ShouldNormalizeTextToAsciiSlug(string input, string expected)
        {
            var result = SlugHelper.GenerateSlug(input);

            result.Should().Be(expected);
        }

        [Fact]
        public void GenerateSlug_ShouldTrimToSixtyCharacters()
        {
            var input = new string('a', 70);

            var result = SlugHelper.GenerateSlug(input);

            result.Should().HaveLength(60);
            result.Should().Be(new string('a', 60));
        }

        [Fact]
        public void GenerateSlug_ShouldReturnEmptyStringForNullOrWhitespace()
        {
            SlugHelper.GenerateSlug(null!).Should().BeEmpty();
            SlugHelper.GenerateSlug("   ").Should().BeEmpty();
        }
    }
}
