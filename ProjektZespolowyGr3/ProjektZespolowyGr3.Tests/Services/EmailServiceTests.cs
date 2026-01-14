using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;
using DomPogrzebowyProjekt.Models.System;

namespace ProjektZespolowyGr3.Tests.Services
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_ShouldNotThrow_WhenConfigurationIsMissing()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c["EmailSettings:SenderEmail"]).Returns((string?)null);
            configuration.Setup(c => c["EmailSettings:SmtpServer"]).Returns((string?)null);
            configuration.Setup(c => c["EmailSettings:Port"]).Returns((string?)null);
            configuration.Setup(c => c["EmailSettings:Password"]).Returns((string?)null);

            var emailService = new EmailService(configuration.Object);

            // Act
            var act = async () => await emailService.SendEmailAsync("test@test.com", "Test", "Body");

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendEmailAsync_ShouldNotThrow_WhenPortIsInvalid()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c["EmailSettings:SenderEmail"]).Returns("sender@test.com");
            configuration.Setup(c => c["EmailSettings:SenderName"]).Returns("Test Sender");
            configuration.Setup(c => c["EmailSettings:SmtpServer"]).Returns("smtp.test.com");
            configuration.Setup(c => c["EmailSettings:Port"]).Returns("invalid");
            configuration.Setup(c => c["EmailSettings:Password"]).Returns("password");

            var emailService = new EmailService(configuration.Object);

            // Act
            var act = async () => await emailService.SendEmailAsync("test@test.com", "Test", "Body");

            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}

