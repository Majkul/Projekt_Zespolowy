using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjektZespolowyGr3.Controllers.User;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class ListingsControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly AuthService _authService;
        private readonly HelperService _helperService;
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly Mock<IGeocodingService> _geocodingServiceMock;
        private readonly Mock<ICardFeeService> _cardFeeServiceMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly ListingsController _controller;

        public ListingsControllerTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _authService = new AuthService(_context);
            _helperService = new HelperService(_context);
            _fileServiceMock = new Mock<IFileService>();
            _geocodingServiceMock = new Mock<IGeocodingService>();
            _cardFeeServiceMock = new Mock<ICardFeeService>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _controller = new ListingsController(_context, _fileServiceMock.Object, _authService, _helperService, _httpContextAccessorMock.Object, _geocodingServiceMock.Object, _cardFeeServiceMock.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task Index_ShouldReturnView_WithListings()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(seller);
            _context.SaveChanges();

            var listing = new Listing
            {
                Title = "Test Listing",
                Description = "Description",
                SellerId = seller.Id,
                Price = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Listings.Add(listing);
            _context.SaveChanges();

            // Act
            var result = await _controller.Index(null, null, null, null, null, null, null, null, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Index_ShouldRequestNavbarSearchHidden()
        {
            // Act
            await _controller.Index(null, null, null, null, null, null, null, null, null);

            // Assert
            _controller.ViewData["HideNavSearch"].Should().Be(true);
        }

        [Fact]
        public async Task Index_ShouldReturnDistinctRows_ForRequestedPages()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(seller);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            _context.Listings.AddRange(
                CreateListing("Newest", seller.Id, now),
                CreateListing("Middle", seller.Id, now.AddMinutes(-1)),
                CreateListing("Oldest", seller.Id, now.AddMinutes(-2)));
            await _context.SaveChangesAsync();

            // Act
            var firstPageResult = await _controller.Index(null, null, null, null, null, "newest", null, null, null, page: 1, pageSize: 2);
            var firstPage = firstPageResult.Should().BeOfType<ViewResult>().Subject.Model
                .Should().BeAssignableTo<IEnumerable<BrowseListingsViewModel>>().Subject.ToList();

            var secondPageResult = await _controller.Index(null, null, null, null, null, "newest", null, null, null, page: 2, pageSize: 2);
            var secondPage = secondPageResult.Should().BeOfType<ViewResult>().Subject.Model
                .Should().BeAssignableTo<IEnumerable<BrowseListingsViewModel>>().Subject.ToList();

            // Assert
            firstPage.Select(m => m.Listing.Title).Should().Equal("Newest", "Middle");
            secondPage.Select(m => m.Listing.Title).Should().Equal("Oldest");
            firstPage.Select(m => m.ListingId).Should().NotIntersectWith(secondPage.Select(m => m.ListingId));
            ((int)_controller.ViewBag.CurrentPage).Should().Be(2);
            ((int)_controller.ViewBag.PageSize).Should().Be(2);
            ((int)_controller.ViewBag.TotalPages).Should().Be(2);
        }

        [Fact]
        public async Task Index_ShouldPreservePagingFilterValuesInViewBag()
        {
            // Arrange
            var tagIds = new List<int> { 1, 2 };

            // Act
            await _controller.Index(null, tagIds, 10, 250, "sale", "price_asc", 15, 52.1, 21.2, page: 3, pageSize: 12);

            // Assert
            ((List<int>)_controller.ViewBag.TagIds).Should().Equal(tagIds);
            ((decimal?)_controller.ViewBag.MinPrice).Should().Be(10);
            ((decimal?)_controller.ViewBag.MaxPrice).Should().Be(250);
            ((string)_controller.ViewBag.ListingType).Should().Be("sale");
            ((string)_controller.ViewBag.SortBy).Should().Be("price_asc");
            ((int?)_controller.ViewBag.MaxDistanceKm).Should().Be(15);
            ((double?)_controller.ViewBag.UserLat).Should().Be(52.1);
            ((double?)_controller.ViewBag.UserLng).Should().Be(21.2);
            ((int)_controller.ViewBag.CurrentPage).Should().Be(1);
            ((int)_controller.ViewBag.PageSize).Should().Be(12);
        }

        [Fact]
        public async Task Index_PriceDescending_ShouldPutPricedListingsBeforeTradeOnlyListings()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(seller);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            _context.Listings.AddRange(
                CreateListing("Trade only", seller.Id, now, price: null),
                CreateListing("Cheap", seller.Id, now.AddMinutes(-1), price: 50),
                CreateListing("Expensive", seller.Id, now.AddMinutes(-2), price: 200));
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index(null, null, null, null, null, "price_desc", null, null, null);
            var listings = result.Should().BeOfType<ViewResult>().Subject.Model
                .Should().BeAssignableTo<IEnumerable<BrowseListingsViewModel>>().Subject.ToList();

            // Assert
            listings.Select(m => m.Listing.Title).Should().Equal("Expensive", "Cheap", "Trade only");
        }

        [Fact]
        public async Task Index_DistanceFilter_ShouldPassCoordinatesAsLongitudeLatitude()
        {
            // Arrange
            var seller = new User
            {
                Username = "seller",
                Email = "seller@test.com",
                CreatedAt = DateTime.UtcNow,
                Latitude = 52.2,
                Longitude = 21.3
            };
            _context.Users.Add(seller);
            await _context.SaveChangesAsync();
            _context.Listings.Add(CreateListing("Nearby", seller.Id, DateTime.UtcNow));
            await _context.SaveChangesAsync();

            _geocodingServiceMock
                .Setup(g => g.CalculateDistanceKm(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(999);
            _geocodingServiceMock
                .Setup(g => g.CalculateDistanceKm(21.2, 52.1, 21.3, 52.2))
                .Returns(5);

            // Act
            var result = await _controller.Index(null, null, null, null, null, null, 10, 52.1, 21.2);
            var listings = result.Should().BeOfType<ViewResult>().Subject.Model
                .Should().BeAssignableTo<IEnumerable<BrowseListingsViewModel>>().Subject.ToList();

            // Assert
            listings.Select(m => m.Listing.Title).Should().ContainSingle().Which.Should().Be("Nearby");
            _geocodingServiceMock.Verify(g => g.CalculateDistanceKm(21.2, 52.1, 21.3, 52.2), Times.Once);
        }

        [Fact]
        public async Task Index_ShouldUseFirstPhoto_WhenListingHasNoFeaturedPhoto()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(seller);
            _context.SaveChanges();

            var upload = new Upload
            {
                FileName = "photo",
                Extension = ".jpg",
                Url = "/uploads/photo.jpg",
                UploaderId = seller.Id
            };
            var listing = new Listing
            {
                Title = "Test Listing",
                Description = "Description",
                SellerId = seller.Id,
                Price = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Photos = new List<ListingPhoto>
                {
                    new() { Upload = upload, IsFeatured = false }
                }
            };
            _context.Listings.Add(listing);
            _context.SaveChanges();

            // Act
            var result = await _controller.Index(null, null, null, null, null, null, null, null, null);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<ProjektZespolowyGr3.Models.ViewModels.BrowseListingsViewModel>>().Subject;
            model.Single().PhotoUrl.Should().Be("/uploads/photo.jpg");
        }

        [Fact(Skip = "ILike nie działa z InMemory database; wymaga prawdziwej bazy PostgreSQL.")]
        public async Task Index_ShouldFilterListings_WhenSearchStringIsProvided()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(seller);
            _context.SaveChanges();

            var listing1 = new Listing
            {
                Title = "Laptop",
                Description = "Gaming laptop",
                SellerId = seller.Id,
                Price = 1000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var listing2 = new Listing
            {
                Title = "Phone",
                Description = "Smartphone",
                SellerId = seller.Id,
                Price = 500,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Listings.AddRange(listing1, listing2);
            _context.SaveChanges();

            // Act
            var result = await _controller.Index("Laptop", null, null, null, null, null, null, null, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Details_ShouldReturnView_WhenListingExists()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(seller);
            _context.SaveChanges();

            var listing = new Listing
            {
                Title = "Test Listing",
                Description = "Description",
                SellerId = seller.Id,
                Price = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Listings.Add(listing);
            _context.SaveChanges();

            // Act
            var result = await _controller.Details("test-listing", listing.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenListingDoesNotExist()
        {
            // Act
            var result = await _controller.Details("missing-listing", 999);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("NotFound");
            _controller.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task DetailsById_ShouldRedirectPermanentlyToSlugRoute_WhenListingExists()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(seller);
            _context.SaveChanges();

            var listing = new Listing
            {
                Title = "Stary rower Trek",
                Description = "Description",
                SellerId = seller.Id,
                Price = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Listings.Add(listing);
            _context.SaveChanges();

            // Act
            var result = await _controller.DetailsById(listing.Id);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = (RedirectToActionResult)result;
            redirectResult.Permanent.Should().BeTrue();
            redirectResult.ActionName.Should().Be("Details");
            redirectResult.RouteValues!["slug"].Should().Be("stary-rower-trek");
            redirectResult.RouteValues["id"].Should().Be(listing.Id);
        }

        [Fact]
        public async Task Create_WhenNotExchangeable_ClearsExchangeFieldsAndPreservesFeaturedState()
        {
            var seller = await AddSellerAsync();
            var tag = new Tag { Name = "Books" };
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            SetupUser(seller.Id);
            var model = new CreateListingViewModel
            {
                Title = "Oferta bez wymiany",
                Price = 100,
                StockQuantity = 1,
                NotExchangeable = true,
                IsFeatured = true,
                MinExchangeValue = 50,
                ExchangeDescription = "Nie powinno zostać zapisane",
                SelectedExchangeAcceptedTagIds = new List<int> { tag.Id }
            };

            var result = await _controller.Create(model, forTradeListingId: null);

            result.Should().BeOfType<RedirectToActionResult>();
            var listing = _context.Listings.Include(l => l.ExchangeAcceptedTags).Single(l => l.Title == "Oferta bez wymiany");
            listing.NotExchangeable.Should().BeTrue();
            listing.IsFeatured.Should().BeTrue();
            listing.MinExchangeValue.Should().BeNull();
            listing.ExchangeDescription.Should().BeNull();
            listing.ExchangeAcceptedTags.Should().BeEmpty();
        }

        [Fact]
        public async Task Create_WhenNotExchangeable_AllowsZeroPrice()
        {
            var seller = await AddSellerAsync();
            SetupUser(seller.Id);
            var model = new CreateListingViewModel
            {
                Title = "Oferta za zero",
                Price = 0,
                StockQuantity = 1,
                NotExchangeable = true
            };

            var result = await _controller.Create(model, forTradeListingId: null);

            result.Should().BeOfType<RedirectToActionResult>();
            _context.Listings.Single(l => l.Title == "Oferta za zero")
                .Price.Should().Be(0);
        }

        [Fact]
        public async Task Create_WhenPhotoFileIsEmpty_ReturnsValidationErrorWithoutSaving()
        {
            var seller = await AddSellerAsync();
            SetupUser(seller.Id);
            _fileServiceMock
                .Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("SaveFileAsync should not be called for empty files."));
            var model = new CreateListingViewModel
            {
                Title = "Oferta z pustym plikiem",
                Price = 100,
                StockQuantity = 1,
                NotExchangeable = true,
                PhotoFiles = new List<IFormFile> { CreateFormFile(length: 0) }
            };

            var result = await _controller.Create(model, forTradeListingId: null);

            result.Should().BeOfType<ViewResult>();
            _controller.ModelState[nameof(CreateListingViewModel.PhotoFiles)]!.Errors
                .Should().Contain(e => e.ErrorMessage.Contains("niedostępne", StringComparison.OrdinalIgnoreCase));
            _context.Listings.Should().BeEmpty();
        }

        private async Task<User> AddSellerAsync()
        {
            var seller = new User
            {
                Username = "seller",
                Email = "seller@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(seller);
            await _context.SaveChangesAsync();
            return seller;
        }

        private static Listing CreateListing(string title, int sellerId, DateTime createdAt, decimal? price = 100)
        {
            return new Listing
            {
                Title = title,
                Description = $"{title} description",
                SellerId = sellerId,
                Price = price,
                StockQuantity = 1,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
        }

        private void SetupUser(int userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

