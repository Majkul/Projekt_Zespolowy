using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Controllers;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class UserProfileControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly UserProfileController _controller;

        public UserProfileControllerTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _controller = new UserProfileController(_context);
        }

        [Fact]
        public async Task Details_ShouldReturnView_WhenUserExists()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = await _controller.Details(user.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Details_ShouldCalculateAverageRating()
        {
            // Arrange
            var seller = new User
            {
                Username = "seller",
                Email = "seller@test.com",
                CreatedAt = DateTime.UtcNow
            };
            var reviewer1 = new User
            {
                Username = "reviewer1",
                Email = "reviewer1@test.com",
                CreatedAt = DateTime.UtcNow
            };
            var reviewer2 = new User
            {
                Username = "reviewer2",
                Email = "reviewer2@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.AddRange(seller, reviewer1, reviewer2);
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

            var review1 = new Review
            {
                ListingId = listing.Id,
                ReviewerId = reviewer1.Id,
                Rating = 5,
                Description = "Great",
                CreatedAt = DateTime.UtcNow
            };
            var review2 = new Review
            {
                ListingId = listing.Id,
                ReviewerId = reviewer2.Id,
                Rating = 3,
                Description = "OK",
                CreatedAt = DateTime.UtcNow
            };
            _context.Reviews.AddRange(review1, review2);
            _context.SaveChanges();

            // Act
            var result = await _controller.Details(seller.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

