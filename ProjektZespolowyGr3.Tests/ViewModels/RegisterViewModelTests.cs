using Xunit;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using ProjektZespolowyGr3.Models.ViewModels;

namespace ProjektZespolowyGr3.Tests.ViewModels
{
    public class RegisterViewModelTests
    {
        [Fact]
        public void RegisterViewModel_ShouldHaveRequiredFields()
        {
            // Arrange
            var model = new RegisterViewModel();

            // Act
            var validationResults = ValidateModel(model);

            // Assert
            validationResults.Should().Contain(v => v.MemberNames.Contains("Login"));
            validationResults.Should().Contain(v => v.MemberNames.Contains("Email"));
            validationResults.Should().Contain(v => v.MemberNames.Contains("Password"));
        }

        [Fact]
        public void RegisterViewModel_ShouldBeValid_WhenAllFieldsAreFilled()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Login = "testuser",
                Email = "test@test.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            // Act
            var validationResults = ValidateModel(model);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void RegisterViewModel_ShouldHaveEmailValidation()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Login = "testuser",
                Email = "invalid-email",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            // Act
            var validationResults = ValidateModel(model);

            // Assert
            validationResults.Should().Contain(v => v.MemberNames.Contains("Email"));
        }

        private IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }
    }
}

