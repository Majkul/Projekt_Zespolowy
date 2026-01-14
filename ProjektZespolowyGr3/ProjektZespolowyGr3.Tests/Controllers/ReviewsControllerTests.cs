using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Moq;
using Microsoft.AspNetCore.Hosting;
using ProjektZespolowyGr3.Controllers.User;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class ReviewsControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly HelperService _helperService;
        private readonly ReviewsController _controller;

        public ReviewsControllerTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns("/wwwroot");
            _helperService = new HelperService(_context);
            _controller = new ReviewsController(_context, _envMock.Object, _helperService);
        }

        private void SetupAuthenticatedUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        [Fact]
        public async Task Create_GET_WithListingId_ShouldReturnView()
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
                Type = ListingType.Sale,
                Price = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Listings.Add(listing);
            _context.SaveChanges();

            // Act
            var result = _controller.Create(listing.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as CreateReviewViewModel;
            model.Should().NotBeNull();
            model!.ListingId.Should().Be(listing.Id);
        }

        [Fact]
        public async Task Create_POST_ShouldCreateReview_WhenModelIsValid()
        {
            // Arrange
            var seller = new User { Username = "seller", Email = "seller@test.com", CreatedAt = DateTime.UtcNow };
            var reviewer = new User { Username = "reviewer", Email = "reviewer@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(seller, reviewer);
            _context.SaveChanges();

            var listing = new Listing
            {
                Title = "Test Listing",
                Description = "Description",
                SellerId = seller.Id,
                Type = ListingType.Sale,
                Price = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Listings.Add(listing);
            _context.SaveChanges();

            SetupAuthenticatedUser(reviewer.Id);

            var model = new CreateReviewViewModel
            {
                ListingId = listing.Id,
                Rating = 5,
                Description = "Great product!"
            };

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _context.Reviews.Any(r => r.Description == "Great product!").Should().BeTrue();
        }

        [Fact]
        public async Task Create_POST_ShouldReturnView_WhenUserIsSeller()
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
                Type = ListingType.Sale,
                Price = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Listings.Add(listing);
            _context.SaveChanges();

            SetupAuthenticatedUser(seller.Id);

            var model = new CreateReviewViewModel
            {
                ListingId = listing.Id,
                Rating = 5,
                Description = "Trying to review own listing"
            };

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            _context.Reviews.Any(r => r.Description == "Trying to review own listing").Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

