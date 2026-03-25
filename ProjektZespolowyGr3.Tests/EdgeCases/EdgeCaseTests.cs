using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Tests.EdgeCases
{
    public class EdgeCaseTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly AuthService _authService;

        public EdgeCaseTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _authService = new AuthService(_context);
        }

        [Fact]
        public void HashPassword_ShouldHandleEmptyPassword()
        {
            // Arrange
            var salt = _authService.GenerateSalt();

            // Act
            var hash = _authService.HashPassword("", salt);

            // Assert
            hash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void HashPassword_ShouldHandleVeryLongPassword()
        {
            // Arrange
            var salt = _authService.GenerateSalt();
            var longPassword = new string('a', 1000);

            // Act
            var hash = _authService.HashPassword(longPassword, salt);

            // Assert
            hash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void HashPassword_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var salt = _authService.GenerateSalt();
            var passwordWithSpecialChars = "P@ssw0rd!#$%^&*()";

            // Act
            var hash = _authService.HashPassword(passwordWithSpecialChars, salt);

            // Assert
            hash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Validate_ShouldHandleNullLogin()
        {
            // Act
            var result = _authService.Validate(null!, "password");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Validate_ShouldHandleNullPassword()
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

            var salt = _authService.GenerateSalt();
            var hashedPassword = _authService.HashPassword("password", salt);
            var userAuth = new UserAuth
            {
                UserId = user.Id,
                Password = hashedPassword,
                PasswordSalt = salt,
                EmailVerified = true
            };
            _context.UserAuths.Add(userAuth);
            _context.SaveChanges();

            // Act
            var result = _authService.Validate("testuser", null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void UserExists_ShouldHandleEmptyString()
        {
            // Act
            var result = _authService.UserExists("");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void EmailTaken_ShouldHandleEmptyString()
        {
            // Act
            var result = _authService.EmailTaken("");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void EmailTaken_ShouldHandleNull()
        {
            // Act
            var result = _authService.EmailTaken(null!);

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

