using System.Security.Claims;
using DomPogrzebowyProjekt.Controllers.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.ViewModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Controllers.Admin;

public class ListingManageControllerTests : IDisposable
{
    private readonly MyDBContext _context;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly ListingManageController _controller;

    public ListingManageControllerTests()
    {
        var options = new DbContextOptionsBuilder<MyDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MyDBContext(options);
        var acc = new Mock<IHttpContextAccessor>();
        _fileServiceMock = new Mock<IFileService>();
        _controller = new ListingManageController(_context, _fileServiceMock.Object, acc.Object);
    }

    public void Dispose() => _context.Dispose();

    private void SetupAdmin()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, "Admin")
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    private async Task<User> AddSellerAsync(string username = "seller")
    {
        var seller = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(seller);
        await _context.SaveChangesAsync();
        return seller;
    }

    private static Listing CreateListing(User seller, string title, Action<Listing>? configure = null)
    {
        var listing = new Listing
        {
            Title = title,
            Description = $"{title} description",
            SellerId = seller.Id,
            Seller = seller,
            Price = 10,
            StockQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        configure?.Invoke(listing);
        return listing;
    }

    private static List<BrowseListingsViewModel> GetListingModel(IActionResult result)
    {
        return result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeAssignableTo<List<BrowseListingsViewModel>>().Subject;
    }

    private static void ShouldRequireAntiForgeryToken(string actionName, params Type[] parameterTypes)
    {
        var method = typeof(ListingManageController).GetMethod(actionName, parameterTypes);

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: true)
            .Should().NotBeEmpty();
    }

    [Fact]
    public async Task Index_ReturnsView_WhenAdmin()
    {
        var u = new User { Username = "s", Email = "s@b.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        _context.Listings.Add(new Listing
        {
            Title = "L",
            SellerId = u.Id,
            Price = 10,
            StockQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        SetupAdmin();

        var result = await _controller.Index();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_UsesPageSizeAsHardLimitWithoutPagination()
    {
        var seller = await AddSellerAsync();
        for (var i = 1; i <= 30; i++)
        {
            _context.Listings.Add(CreateListing(seller, $"Listing {i:00}"));
        }
        await _context.SaveChangesAsync();
        SetupAdmin();

        var firstPage = GetListingModel(await _controller.Index(pageSize: 10, pageNumber: 1));
        var secondPage = GetListingModel(await _controller.Index(pageSize: 10, pageNumber: 2));

        firstPage.Should().HaveCount(10);
        secondPage.Select(x => x.ListingId).Should().Equal(firstPage.Select(x => x.ListingId));
    }

    [Fact]
    public async Task Index_TagFilter_UsesRealListingTags()
    {
        var seller = await AddSellerAsync();
        var furniture = new Tag { Name = "Furniture" };
        var garden = new Tag { Name = "Garden" };
        var furnitureListing = CreateListing(seller, "Wooden desk");
        var gardenListing = CreateListing(seller, "Garden chair");
        furnitureListing.Tags.Add(new ListingTag { Listing = furnitureListing, Tag = furniture });
        gardenListing.Tags.Add(new ListingTag { Listing = gardenListing, Tag = garden });
        _context.Listings.AddRange(furnitureListing, gardenListing);
        await _context.SaveChangesAsync();
        SetupAdmin();

        var result = await _controller.Index(tagFilter: "Furniture");
        var model = GetListingModel(result);

        model.Should().ContainSingle(x => x.Listing.Title == "Wooden desk");
        model.Should().NotContain(x => x.Listing.Title == "Garden chair");
        result.Should().BeOfType<ViewResult>().Subject.ViewData["AllTags"]
            .Should().BeEquivalentTo(new[] { "Furniture", "Garden" });
    }

    [Fact]
    public async Task Index_ShowArchived_IncludesArchivedDeletedSoldAndOutOfStockListings()
    {
        var seller = await AddSellerAsync();
        _context.Listings.AddRange(
            CreateListing(seller, "Active"),
            CreateListing(seller, "Archived", l => l.IsArchived = true),
            CreateListing(seller, "Deleted", l => l.IsDeleted = true),
            CreateListing(seller, "Sold", l => l.IsSold = true),
            CreateListing(seller, "Out of stock", l => l.StockQuantity = 0));
        await _context.SaveChangesAsync();
        SetupAdmin();

        var archived = GetListingModel(await _controller.Index(showArchived: true, pageSize: 10));
        var active = GetListingModel(await _controller.Index(showArchived: false, pageSize: 10));

        archived.Select(x => x.Listing.Title).Should().BeEquivalentTo("Archived", "Deleted", "Sold", "Out of stock");
        active.Should().ContainSingle(x => x.Listing.Title == "Active");
    }

    [Fact]
    public async Task Index_SearchesCaseInsensitiveByIdTitleAndSellerUsername()
    {
        var seller = await AddSellerAsync("sellerCase");
        var listing = CreateListing(seller, "Unique title");
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupAdmin();

        var byId = GetListingModel(await _controller.Index(searchString: listing.Id.ToString()));
        var byTitle = GetListingModel(await _controller.Index(searchString: "unique"));
        var bySeller = GetListingModel(await _controller.Index(searchString: "SELLERCASE"));

        byId.Should().ContainSingle(x => x.ListingId == listing.Id);
        byTitle.Should().ContainSingle(x => x.ListingId == listing.Id);
        bySeller.Should().ContainSingle(x => x.ListingId == listing.Id);
    }

    [Fact]
    public void PostActions_RequireAntiForgeryToken()
    {
        ShouldRequireAntiForgeryToken(nameof(ListingManageController.EditListing), typeof(int), typeof(EditListingViewModel), typeof(string));
        ShouldRequireAntiForgeryToken(nameof(ListingManageController.RestoreListing), typeof(int));
        ShouldRequireAntiForgeryToken(nameof(ListingManageController.DeleteListing), typeof(int), typeof(string));
    }

    [Fact]
    public async Task RestoreListing_RestoresOnlyListingsThatCanBecomeActive()
    {
        var seller = await AddSellerAsync();
        var restorable = CreateListing(seller, "Restorable", l => l.IsArchived = true);
        var deleted = CreateListing(seller, "Deleted", l => { l.IsArchived = true; l.IsDeleted = true; });
        var sold = CreateListing(seller, "Sold", l => { l.IsArchived = true; l.IsSold = true; });
        var outOfStock = CreateListing(seller, "Out of stock", l => { l.IsArchived = true; l.StockQuantity = 0; });
        _context.Listings.AddRange(restorable, deleted, sold, outOfStock);
        await _context.SaveChangesAsync();
        SetupAdmin();

        _controller.RestoreListing(restorable.Id).Should().BeOfType<RedirectToActionResult>();
        _controller.RestoreListing(deleted.Id).Should().BeOfType<BadRequestResult>();
        _controller.RestoreListing(sold.Id).Should().BeOfType<BadRequestResult>();
        _controller.RestoreListing(outOfStock.Id).Should().BeOfType<BadRequestResult>();

        restorable.IsArchived.Should().BeFalse();
        deleted.IsArchived.Should().BeTrue();
        sold.IsArchived.Should().BeTrue();
        outOfStock.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task EditListing_WhenNotExchangeable_ClearsExchangeFieldsAndAcceptedTags()
    {
        var seller = await AddSellerAsync();
        var tag = new Tag { Name = "Books" };
        var listing = CreateListing(seller, "Trade listing", l =>
        {
            l.NotExchangeable = false;
            l.MinExchangeValue = 50;
            l.ExchangeDescription = "Książki";
            l.ExchangeAcceptedTags.Add(new ListingExchangeAcceptedTag { Tag = tag, Listing = l });
        });
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupAdmin();

        var vm = EditVmFrom(listing);
        vm.NotExchangeable = true;
        vm.MinExchangeValue = 100;
        vm.ExchangeDescription = "Nie powinno zostać zapisane";
        vm.SelectedExchangeAcceptedTagIds = new List<int> { tag.Id };

        await _controller.EditListing(listing.Id, vm);

        listing.MinExchangeValue.Should().BeNull();
        listing.ExchangeDescription.Should().BeNull();
        listing.ExchangeAcceptedTags.Should().BeEmpty();
    }

    [Fact]
    public async Task EditListing_WhenPhotoFileIsEmpty_ReturnsValidationErrorWithoutSaving()
    {
        var seller = await AddSellerAsync();
        var listing = CreateListing(seller, "Photo-less listing");
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupAdmin();
        _fileServiceMock
            .Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("SaveFileAsync should not be called for empty files."));
        var vm = EditVmFrom(listing);
        vm.PhotoFiles = new List<IFormFile> { CreateFormFile(length: 0) };

        var result = await _controller.EditListing(listing.Id, vm);

        result.Should().BeOfType<ViewResult>();
        _controller.ModelState[nameof(EditListingViewModel.PhotoFiles)]!.Errors
            .Should().Contain(e => e.ErrorMessage.Contains("niedostępne", StringComparison.OrdinalIgnoreCase));
        listing.Photos.Should().BeEmpty();
    }

    [Fact]
    public async Task EditListing_AddsPhotoToListingWithoutPhotos()
    {
        var seller = await AddSellerAsync();
        var listing = CreateListing(seller, "Photo-less listing");
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupAdmin();
        _fileServiceMock
            .Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), seller.Id))
            .ReturnsAsync(new Upload
            {
                FileName = "new.jpg",
                Extension = ".jpg",
                Url = "/uploads/new.jpg",
                UploaderId = seller.Id
            });
        var vm = EditVmFrom(listing);
        vm.PhotoFiles = new List<IFormFile> { CreateFormFile(length: 10) };

        var result = await _controller.EditListing(listing.Id, vm);

        result.Should().BeOfType<RedirectToActionResult>();
        listing.Photos.Should().ContainSingle(p => p.Upload.Url == "/uploads/new.jpg");
    }

    [Fact]
    public async Task EditListing_WithLocalReturnUrl_RedirectsBackToReturnUrl()
    {
        var seller = await AddSellerAsync();
        var listing = CreateListing(seller, "Return listing");
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupAdmin();

        var result = await _controller.EditListing(listing.Id, EditVmFrom(listing), "/Listings/return-listing-1");

        result.Should().BeOfType<LocalRedirectResult>()
            .Subject.Url.Should().Be("/Listings/return-listing-1");
    }

    [Fact]
    public async Task DeleteListing_WithUnsafeReturnUrl_FallsBackToIndex()
    {
        var seller = await AddSellerAsync();
        var listing = CreateListing(seller, "Delete listing");
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupAdmin();

        var result = _controller.DeleteListing(listing.Id, "https://example.com/not-local");

        result.Should().BeOfType<RedirectToActionResult>()
            .Subject.ActionName.Should().Be("Index");
    }

    private static EditListingViewModel EditVmFrom(Listing listing)
    {
        return new EditListingViewModel
        {
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            StockQuantity = listing.StockQuantity,
            NotExchangeable = listing.NotExchangeable,
            IsPrivate = listing.IsPrivate,
            IsFeatured = listing.IsFeatured,
            MinExchangeValue = listing.MinExchangeValue,
            ExchangeDescription = listing.ExchangeDescription,
            SelectedTagIds = listing.Tags.Select(t => t.TagId).ToList(),
            SelectedExchangeAcceptedTagIds = listing.ExchangeAcceptedTags.Select(t => t.TagId).ToList()
        };
    }

    private static IFormFile CreateFormFile(long length)
    {
        var stream = length > 0 ? new MemoryStream(new byte[length]) : new MemoryStream();
        return new FormFile(stream, 0, length, "PhotoFiles", "photo.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }
}
