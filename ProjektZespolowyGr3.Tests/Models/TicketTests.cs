using Xunit;
using FluentAssertions;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Tests.Models
{
    public class TicketTests
    {
        [Fact]
        public void Ticket_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var ticket = new Ticket
            {
                Id = 1,
                UserId = 1,
                Category = TicketCategory.Other_Issue,
                Status = TicketStatus.Open,
                Subject = "Test Subject",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            // Assert
            ticket.Id.Should().Be(1);
            ticket.UserId.Should().Be(1);
            ticket.Category.Should().Be(TicketCategory.Other_Issue);
            ticket.Status.Should().Be(TicketStatus.Open);
            ticket.Subject.Should().Be("Test Subject");
            ticket.Description.Should().Be("Test Description");
        }

        [Fact]
        public void Ticket_ShouldSupportOptionalAssignee()
        {
            // Arrange & Act
            var ticket = new Ticket
            {
                UserId = 1,
                Category = TicketCategory.Other_Issue,
                Status = TicketStatus.Open,
                Subject = "Test",
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                AssigneeId = 5
            };

            // Assert
            ticket.AssigneeId.Should().Be(5);
        }

        [Fact]
        public void Ticket_ShouldSupportOptionalReportedUser()
        {
            // Arrange & Act
            var ticket = new Ticket
            {
                UserId = 1,
                Category = TicketCategory.User_Report,
                Status = TicketStatus.Open,
                Subject = "Test",
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                ReportedUserId = 10
            };

            // Assert
            ticket.ReportedUserId.Should().Be(10);
        }

        [Fact]
        public void Ticket_ShouldSupportOptionalReportedListing()
        {
            // Arrange & Act
            var ticket = new Ticket
            {
                UserId = 1,
                Category = TicketCategory.Listing_Report,
                Status = TicketStatus.Open,
                Subject = "Test",
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                ReportedListingId = 15
            };

            // Assert
            ticket.ReportedListingId.Should().Be(15);
        }
    }
}

