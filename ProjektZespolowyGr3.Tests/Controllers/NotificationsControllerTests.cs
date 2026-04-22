using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjektZespolowyGr3.Controllers.User;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Controllers;

public class NotificationsControllerTests : IDisposable
{
    private readonly MyDBContext _context;
    private readonly Mock<IPayuOrderSyncService> _payu = new();
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<MyDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MyDBContext(options);
        _payu.Setup(p => p.SyncPendingOrdersForSellerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _controller = new NotificationsController(_context, _payu.Object);
    }

    public void Dispose() => _context.Dispose();

    private void SetupUser(int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "u")
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    [Fact]
    public async Task Index_CallsSync_AndReturnsView()
    {
        var u = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        SetupUser(u.Id);

        var result = await _controller.Index();

        result.Should().BeOfType<ViewResult>();
        _payu.Verify(p => p.SyncPendingOrdersForSellerAsync(u.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Go_MarksRead_AndRedirectsToTradeProposal_WhenKindTrade()
    {
        var u = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        var other = new User { Username = "o", Email = "o@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(u, other);
        await _context.SaveChangesAsync();

        var listing = new Listing
        {
            Title = "L",
            SellerId = other.Id,
            Price = 1,
            StockQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var tp = new TradeProposal
        {
            InitiatorUserId = other.Id,
            ReceiverUserId = u.Id,
            SubjectListingId = listing.Id,
            Status = TradeProposalStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            LastModifiedAt = now
        };
        _context.TradeProposals.Add(tp);
        await _context.SaveChangesAsync();

        var n = new Notification
        {
            UserId = u.Id,
            Kind = NotificationKind.TradeProposalReceived,
            CreatedAt = now,
            IsRead = false,
            TradeProposalId = tp.Id
        };
        _context.Notifications.Add(n);
        await _context.SaveChangesAsync();

        SetupUser(u.Id);
        var result = await _controller.Go(n.Id);

        var red = result.Should().BeOfType<RedirectToActionResult>().Subject;
        red.ActionName.Should().Be("Details");
        red.ControllerName.Should().Be("TradeProposals");
        (await _context.Notifications.AsNoTracking().FirstAsync(x => x.Id == n.Id)).IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllRead_MarksUserNotifications()
    {
        var u = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();

        _context.Notifications.AddRange(
            new Notification { UserId = u.Id, Kind = NotificationKind.NewMessage, CreatedAt = DateTime.UtcNow, IsRead = false },
            new Notification { UserId = u.Id, Kind = NotificationKind.NewMessage, CreatedAt = DateTime.UtcNow, IsRead = false });
        await _context.SaveChangesAsync();

        SetupUser(u.Id);
        await _controller.MarkAllRead();

        (await _context.Notifications.AllAsync(n => n.UserId != u.Id || n.IsRead)).Should().BeTrue();
    }
}
