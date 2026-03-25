using Xunit;
using FluentAssertions;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;
using Microsoft.EntityFrameworkCore;

namespace ProjektZespolowyGr3.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _authService = new AuthService(_context);
        }

        [Fact]
        public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
        {
            // Arrange
            var password = "TestPassword123";
            var salt1 = _authService.GenerateSalt();
            var salt2 = _authService.GenerateSalt();

            // Act
            var hash1 = _authService.HashPassword(password, salt1);
            var hash2 = _authService.HashPassword(password, salt2);

            // Assert
            hash1.Should().NotBe(hash2);
            hash1.Should().NotBeNullOrEmpty();
            hash2.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void HashPassword_ShouldGenerateSameHashForSamePasswordAndSalt()
        {
            // Arrange
            var password = "TestPassword123";
            var salt = _authService.GenerateSalt();

            // Act
            var hash1 = _authService.HashPassword(password, salt);
            var hash2 = _authService.HashPassword(password, salt);

            // Assert
            hash1.Should().Be(hash2);
        }

        [Fact]
        public void GenerateSalt_ShouldGenerateUniqueSalts()
        {
            // Act
            var salt1 = _authService.GenerateSalt();
            var salt2 = _authService.GenerateSalt();

            // Assert
            salt1.Should().NotBe(salt2);
            salt1.Should().NotBeNullOrEmpty();
            salt2.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Validate_ShouldReturnUser_WhenCredentialsAreCorrect()
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

            var password = "TestPassword123";
            var salt = _authService.GenerateSalt();
            var hashedPassword = _authService.HashPassword(password, salt);

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
            var result = _authService.Validate("testuser", password);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("testuser");
        }

        [Fact]
        public void Validate_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Act
            var result = _authService.Validate("nonexistent", "password");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Validate_ShouldReturnNull_WhenPasswordIsIncorrect()
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
            var hashedPassword = _authService.HashPassword("CorrectPassword", salt);

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
            var result = _authService.Validate("testuser", "WrongPassword");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Validate_ShouldReturnNull_WhenUserAuthDoesNotExist()
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

            // Act
            var result = _authService.Validate("testuser", "password");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void UserExists_ShouldReturnTrue_WhenUserExists()
        {
            // Arrange
            var user = new User
            {
                Username = "existinguser",
                Email = "existing@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _authService.UserExists("existinguser");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void UserExists_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Act
            var result = _authService.UserExists("nonexistentuser");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void EmailTaken_ShouldReturnTrue_WhenEmailExists()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "taken@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _authService.EmailTaken("taken@test.com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void EmailTaken_ShouldReturnFalse_WhenEmailDoesNotExist()
        {
            // Act
            var result = _authService.EmailTaken("free@test.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetClaims_ShouldCreateClaims_ForRegularUser()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@test.com",
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var claims = _authService.GetClaims(user);

            // Assert
            claims.Should().NotBeNull();
            claims.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value.Should().Be("testuser");
            claims.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value.Should().Be("1");
            claims.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value.Should().Be("Client");
        }

        [Fact]
        public void GetClaims_ShouldCreateClaims_ForAdminUser()
        {
            // Arrange
            var user = new User
            {
                Id = 2,
                Username = "admin",
                Email = "admin@test.com",
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var claims = _authService.GetClaims(user);

            // Assert
            claims.Should().NotBeNull();
            claims.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value.Should().Be("Admin");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

