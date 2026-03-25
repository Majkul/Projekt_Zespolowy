using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ProjektZespolowyGr3.Controllers;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;
using DomPogrzebowyProjekt.Models.System;

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class HomeControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly Mock<ILogger<HomeController>> _loggerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly AuthService _authService;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _loggerMock = new Mock<ILogger<HomeController>>();
            _emailServiceMock = new Mock<IEmailService>();
            _authService = new AuthService(_context);
            _controller = new HomeController(_loggerMock.Object, _context, _authService, _emailServiceMock.Object);
        }

        [Fact]
        public async Task Index_ShouldReturnView_WithLatestListings()
        {
            // Arrange
            var seller = new User
            {
                Username = "seller",
                Email = "seller@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(seller);
            _context.SaveChanges();

            var listings = new List<Listing>
            {
                new Listing
                {
                    Title = "Listing 1",
                    Description = "Description 1",
                    SellerId = seller.Id,
                    Seller = seller,
                    Type = ListingType.Sale,
                    Price = 100,
                    IsSold = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                },
                new Listing
                {
                    Title = "Listing 2",
                    Description = "Description 2",
                    SellerId = seller.Id,
                    Seller = seller,
                    Type = ListingType.Sale,
                    Price = 200,
                    IsSold = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
            _context.Listings.AddRange(listings);
            _context.SaveChanges();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
        }

        [Fact]
        public void Privacy_ShouldReturnView()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Login_GET_ShouldReturnView()
        {
            // Arrange
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Method = "GET";

            // Act
            var result = await _controller.Login("", "", null);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public void Register_GET_ShouldReturnView()
        {
            // Act
            var result = _controller.Register();

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Register_POST_ShouldCreateUser_WhenModelIsValid()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Login = "newuser",
                Email = "newuser@test.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock
                .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("https://localhost/Account/VerifyEmail?email=test&token=test");

            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Scheme = "https";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
            _controller.Url = urlHelperMock.Object;

            // Act
            var result = await _controller.Register(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _context.Users.Any(u => u.Username == "newuser").Should().BeTrue();
            _context.UserAuths.Any(ua => ua.UserId == _context.Users.First(u => u.Username == "newuser").Id).Should().BeTrue();
        }

        [Fact]
        public async Task Register_POST_ShouldReturnView_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Login = "newuser",
                Email = "newuser@test.com",
                Password = "Password123",
                ConfirmPassword = "DifferentPassword"
            };

            // Act
            var result = await _controller.Register(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Register_POST_ShouldReturnView_WhenUserAlreadyExists()
        {
            // Arrange
            var existingUser = new User
            {
                Username = "existinguser",
                Email = "existing@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            _context.SaveChanges();

            var model = new RegisterViewModel
            {
                Login = "existinguser",
                Email = "new@test.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            // Act
            var result = await _controller.Register(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Register_POST_ShouldReturnView_WhenEmailIsTaken()
        {
            // Arrange
            var existingUser = new User
            {
                Username = "user1",
                Email = "taken@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            _context.SaveChanges();

            var model = new RegisterViewModel
            {
                Login = "newuser",
                Email = "taken@test.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");
            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpContext
            };
            _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

            // Act
            var result = await _controller.Register(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

