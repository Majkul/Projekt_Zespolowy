using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using ProjektZespolowyGr3.Models.ViewModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.ViewModels;

public class ListingViewModelValidationTests
{
    [Theory]
    [InlineData("-0.01")]
    [InlineData("1000000.01")]
    public void CreateListingViewModel_RejectsPriceOutsideMarketplaceLimits(string price)
    {
        var model = ValidCreateModel();
        model.Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(CreateListingViewModel.Price)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1000000")]
    public void CreateListingViewModel_AcceptsPriceBoundaryValues(string price)
    {
        var model = ValidCreateModel();
        model.Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        var errors = Validate(model);

        errors.Should().NotContain(e => e.MemberNames.Contains(nameof(CreateListingViewModel.Price)));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1000000.01")]
    public void EditListingViewModel_RejectsMinimumExchangeValueOutsideMarketplaceLimits(string value)
    {
        var model = ValidEditModel();
        model.MinExchangeValue = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(EditListingViewModel.MinExchangeValue)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1000000")]
    public void EditListingViewModel_AcceptsMinimumExchangeValueBoundaryValues(string value)
    {
        var model = ValidEditModel();
        model.MinExchangeValue = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        var errors = Validate(model);

        errors.Should().NotContain(e => e.MemberNames.Contains(nameof(EditListingViewModel.MinExchangeValue)));
    }

    private static CreateListingViewModel ValidCreateModel()
    {
        return new CreateListingViewModel
        {
            Title = "Oferta testowa",
            Price = 10,
            StockQuantity = 1
        };
    }

    private static EditListingViewModel ValidEditModel()
    {
        return new EditListingViewModel
        {
            Title = "Oferta testowa",
            Price = 10,
            StockQuantity = 1
        };
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
