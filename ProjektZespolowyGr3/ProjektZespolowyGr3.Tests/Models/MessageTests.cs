using Xunit;
using FluentAssertions;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Tests.Models
{
    public class MessageTests
    {
        [Fact]
        public void Message_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var message = new Message
            {
                Id = 1,
                SenderId = 1,
                ReceiverId = 2,
                Content = "Test message",
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            // Assert
            message.Id.Should().Be(1);
            message.SenderId.Should().Be(1);
            message.ReceiverId.Should().Be(2);
            message.Content.Should().Be("Test message");
            message.IsRead.Should().BeFalse();
        }

        [Fact]
        public void Message_ShouldSupportOptionalListingId()
        {
            // Arrange & Act
            var message = new Message
            {
                SenderId = 1,
                ReceiverId = 2,
                Content = "Test",
                ListingId = 5,
                SentAt = DateTime.UtcNow
            };

            // Assert
            message.ListingId.Should().Be(5);
        }

        [Fact]
        public void Message_ShouldSupportOptionalTicketId()
        {
            // Arrange & Act
            var message = new Message
            {
                SenderId = 1,
                ReceiverId = 2,
                Content = "Test",
                TicketId = 10,
                SentAt = DateTime.UtcNow
            };

            // Assert
            message.TicketId.Should().Be(10);
        }
    }
}

