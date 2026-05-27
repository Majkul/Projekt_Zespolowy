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

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class ListingsControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly AuthService _authService;
        private readonly HelperService _helperService;
        private readonly IFileService _fileService;
<<<<<<< HEAD
=======
        private readonly IGeocodingService _geocodingService;
>>>>>>> origin/main
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
            _fileService = new Mock<IFileService>().Object;
<<<<<<< HEAD
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _controller = new ListingsController(_context, _fileService, _authService, _helperService, _httpContextAccessorMock.Object);
=======
            _geocodingService = new Mock<IGeocodingService>().Object;
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _controller = new ListingsController(_context, _fileService, _authService, _helperService, _httpContextAccessorMock.Object, _geocodingService);
<<<<<<< HEAD
>>>>>>> origin/main
=======
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
>>>>>>> origin/UI_improvements
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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

