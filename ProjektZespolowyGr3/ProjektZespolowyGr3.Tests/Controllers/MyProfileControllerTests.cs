using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using ProjektZespolowyGr3.Controllers;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class MyProfileControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly MyProfileController _controller;

        public MyProfileControllerTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _controller = new MyProfileController(_context);
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
            var httpContext = new DefaultHttpContext
            {
                User = principal
            };
            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpContext
            };
            _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
        }

        [Fact]
        public async Task Edit_GET_ShouldReturnView_WithUserData()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User",
                Address = "Test Address",
                PhoneNumber = "123456789",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            SetupAuthenticatedUser(user.Id);

            // Act
            var result = await _controller.Edit();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as EditMyProfileViewModel;
            model.Should().NotBeNull();
            model!.FirstName.Should().Be("Test");
            model.LastName.Should().Be("User");
        }

        [Fact]
        public async Task Edit_GET_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal()
                }
            };

            // Act
            var result = await _controller.Edit();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task Edit_POST_ShouldUpdateUser_WhenModelIsValid()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@test.com",
                FirstName = "Old",
                LastName = "Name",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            SetupAuthenticatedUser(user.Id);

            var model = new EditMyProfileViewModel
            {
                FirstName = "New",
                LastName = "Name",
                Address = "New Address",
                PhoneNumber = "987654321",
                Email = "newemail@test.com"
            };

            // Act
            var result = await _controller.Edit(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.FirstName.Should().Be("New");
            updatedUser.LastName.Should().Be("Name");
            updatedUser.Address.Should().Be("New Address");
            updatedUser.PhoneNumber.Should().Be("987654321");
        }

        [Fact]
        public async Task Edit_POST_ShouldReturnView_WhenEmailIsTaken()
        {
            // Arrange
            var user1 = new User
            {
                Username = "user1",
                Email = "user1@test.com",
                CreatedAt = DateTime.UtcNow
            };
            var user2 = new User
            {
                Username = "user2",
                Email = "user2@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            SetupAuthenticatedUser(user1.Id);

            var model = new EditMyProfileViewModel
            {
                FirstName = "Test",
                LastName = "User",
                Email = "user2@test.com" // Email już zajęty przez user2
            };

            // Act
            var result = await _controller.Edit(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Details_ShouldRedirectToUserProfile()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            SetupAuthenticatedUser(user.Id);

            // Act
            var result = await _controller.Details();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Details");
            redirectResult.ControllerName.Should().Be("UserProfile");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

