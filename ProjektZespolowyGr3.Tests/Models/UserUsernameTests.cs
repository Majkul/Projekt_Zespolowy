using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Models
{
    public class UserUsernameTests
    {
        [Theory]
        [InlineData("jan_kowalski", true)]
        [InlineData("Jan123", true)]
        [InlineData("jan-kowalski", false)]
        [InlineData("jan kowalski", false)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
        public void Username_ShouldAllowOnlyLettersDigitsAndUnderscoreUpToThirtyTwoCharacters(string username, bool expectedValid)
        {
            var user = new User { Username = username, Email = "test@example.com" };
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(
                user,
                new ValidationContext(user),
                validationResults,
                validateAllProperties: true);

            isValid.Should().Be(expectedValid);
        }

        [Fact]
        public void Model_ShouldConfigureUsernameAsRequiredMaxThirtyTwoAndUnique()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new MyDBContext(options);
            var userEntity = context.Model.FindEntityType(typeof(User));
            var usernameProperty = userEntity!.FindProperty(nameof(User.Username));
            var usernameIndex = userEntity.GetIndexes()
                .SingleOrDefault(index => index.Properties.Any(property => property.Name == nameof(User.Username)));

            usernameProperty.Should().NotBeNull();
            usernameProperty!.IsNullable.Should().BeFalse();
            usernameProperty.GetMaxLength().Should().Be(32);
            usernameIndex.Should().NotBeNull();
            usernameIndex!.IsUnique.Should().BeTrue();
        }
    }
}
