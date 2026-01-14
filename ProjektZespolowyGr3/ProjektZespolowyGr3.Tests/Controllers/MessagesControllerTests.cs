using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ProjektZespolowyGr3.Controllers.User;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Tests.Controllers
{
    public class MessagesControllerTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly MessagesController _controller;

        public MessagesControllerTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _controller = new MessagesController(_context);
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
        public async Task Index_ShouldReturnView_WithConversations()
        {
            // Arrange
            var user1 = new User { Username = "user1", Email = "user1@test.com", CreatedAt = DateTime.UtcNow };
            var user2 = new User { Username = "user2", Email = "user2@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            var message = new Message
            {
                SenderId = user1.Id,
                ReceiverId = user2.Id,
                Content = "Test message",
                SentAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            _context.SaveChanges();

            SetupAuthenticatedUser(user1.Id);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Conversation_ShouldReturnView_WithMessages()
        {
            // Arrange
            var user1 = new User { Username = "user1", Email = "user1@test.com", CreatedAt = DateTime.UtcNow };
            var user2 = new User { Username = "user2", Email = "user2@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            var message = new Message
            {
                SenderId = user1.Id,
                ReceiverId = user2.Id,
                Content = "Test message",
                SentAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            _context.SaveChanges();

            SetupAuthenticatedUser(user1.Id);

            // Act
            var result = await _controller.Conversation(user2.Id, null, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Conversation_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Conversation(999, null, null);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Send_ShouldCreateMessage_WhenContentIsValid()
        {
            // Arrange
            var user1 = new User { Username = "user1", Email = "user1@test.com", CreatedAt = DateTime.UtcNow };
            var user2 = new User { Username = "user2", Email = "user2@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            SetupAuthenticatedUser(user1.Id);

            // Act
            var result = await _controller.Send(user2.Id, "Test message content", null, null);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _context.Messages.Any(m => m.Content == "Test message content").Should().BeTrue();
        }

        [Fact]
        public async Task Send_ShouldRedirect_WhenContentIsEmpty()
        {
            // Arrange
            var user1 = new User { Username = "user1", Email = "user1@test.com", CreatedAt = DateTime.UtcNow };
            var user2 = new User { Username = "user2", Email = "user2@test.com", CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            SetupAuthenticatedUser(user1.Id);

            // Act
            var result = await _controller.Send(user2.Id, "", null, null);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

