using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Tests.Integration
{
    public class UserRegistrationFlowTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly AuthService _authService;

        public UserRegistrationFlowTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _authService = new AuthService(_context);
        }

        [Fact]
        public void CompleteRegistrationFlow_ShouldWork()
        {
            // Arrange
            var username = "newuser";
            var email = "newuser@test.com";
            var password = "Password123";

            // Act - Step 1: Check if user exists (should be false)
            var userExistsBefore = _authService.UserExists(username);
            var emailTakenBefore = _authService.EmailTaken(email);

            // Step 2: Create user
            var user = new User
            {
                Username = username,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Step 3: Create user auth
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

            // Step 4: Validate login
            var validatedUser = _authService.Validate(username, password);

            // Assert
            userExistsBefore.Should().BeFalse();
            emailTakenBefore.Should().BeFalse();
            validatedUser.Should().NotBeNull();
            validatedUser!.Username.Should().Be(username);
            validatedUser.Email.Should().Be(email);
        }

        [Fact]
        public void RegistrationFlow_ShouldPreventDuplicateUsernames()
        {
            // Arrange
            var username = "duplicate";
            var user1 = new User
            {
                Username = username,
                Email = "user1@test.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user1);
            _context.SaveChanges();

            // Act
            var userExists = _authService.UserExists(username);

            // Assert
            userExists.Should().BeTrue();
        }

        [Fact]
        public void RegistrationFlow_ShouldPreventDuplicateEmails()
        {
            // Arrange
            var email = "duplicate@test.com";
            var user1 = new User
            {
                Username = "user1",
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user1);
            _context.SaveChanges();

            // Act
            var emailTaken = _authService.EmailTaken(email);

            // Assert
            emailTaken.Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

