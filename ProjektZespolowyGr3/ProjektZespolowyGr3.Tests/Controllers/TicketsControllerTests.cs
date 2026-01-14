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
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;
using ProjektZespolowyGr3.Models.System;

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class TicketsControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly HelperService _helperService;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly TicketsController _controller;

        public TicketsControllerTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _helperService = new HelperService(_context);
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns("/wwwroot");
            _controller = new TicketsController(_context, _helperService, _envMock.Object);
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
        public async Task Index_ShouldReturnOnlyUserTickets()
        {
            // Arrange
            var user1 = new User { Username = "user1", Email = "user1@test.com", CreatedAt = DateTime.UtcNow };
            var user2 = new User { Username = "user2", Email = "user2@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            var ticket1 = new Ticket
            {
                UserId = user1.Id,
                Category = TicketCategory.Other_Issue,
                Status = TicketStatus.Open,
                Subject = "User1 Ticket",
                Description = "Description",
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };
            var ticket2 = new Ticket
            {
                UserId = user2.Id,
                Category = TicketCategory.Other_Issue,
                Status = TicketStatus.Open,
                Subject = "User2 Ticket",
                Description = "Description",
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };
            _context.Tickets.AddRange(ticket1, ticket2);
            _context.SaveChanges();

            SetupAuthenticatedUser(user1.Id);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<Ticket>;
            model.Should().HaveCount(1);
            model!.First().Subject.Should().Be("User1 Ticket");
        }

        [Fact]
        public void ReportUser_ShouldReturnView_WithPreFilledViewModel()
        {
            // Arrange
            var reportedUser = new User { Username = "reported", Email = "reported@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(reportedUser);
            _context.SaveChanges();

            // Act
            var result = _controller.ReportUser(reportedUser.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as CreateTicketViewModel;
            model.Should().NotBeNull();
            model!.Category.Should().Be(TicketCategory.User_Report);
            model.ReportedUserId.Should().Be(reportedUser.Id);
        }

        [Fact]
        public void ReportListing_ShouldReturnView_WithPreFilledViewModel()
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
            var result = _controller.ReportListing(listing.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as CreateTicketViewModel;
            model.Should().NotBeNull();
            model!.Category.Should().Be(TicketCategory.Listing_Report);
            model.ReportedListingId.Should().Be(listing.Id);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

