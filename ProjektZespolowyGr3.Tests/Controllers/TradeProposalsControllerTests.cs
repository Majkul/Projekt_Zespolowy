using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Moq;
using ProjektZespolowyGr3.Controllers.User;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Controllers;

public class TradeProposalsControllerTests : IDisposable
{
    private readonly MyDBContext _context;
    private readonly Mock<INotificationService> _notifications = new();
    private readonly TradeProposalsController _controller;

    public TradeProposalsControllerTests()
    {
        var options = new DbContextOptionsBuilder<MyDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MyDBContext(options);
        _notifications
            .Setup(n => n.NotifyTradeProposalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _controller = new TradeProposalsController(_context, _notifications.Object);
    }

    public void Dispose() => _context.Dispose();

    private void SetupHttpContext(int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "test")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var form = new FormCollection(new Dictionary<string, StringValues>());
        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.Form).Returns(form);
        var httpMock = new Mock<HttpContext>();
        httpMock.SetupGet(c => c.User).Returns(principal);
        httpMock.SetupGet(c => c.Request).Returns(requestMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpMock.Object };
    }

    private static Listing NewExchangeListing(int sellerId, string title, int stock = 1, decimal? price = 40)
    {
        var now = DateTime.UtcNow;
        return new Listing
        {
            Title = title,
            Description = "d",
            SellerId = sellerId,
            Type = ListingType.Trade,
            Price = price,
            StockQuantity = stock,
            NotExchangeable = false,
            IsArchived = false,
            IsSold = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    [Fact]
    public async Task Compose_ReturnsNotFound_WhenListingMissing()
    {
        var u = new User { Username = "a", Email = "a@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        SetupHttpContext(u.Id);

        var result = await _controller.Compose(999, null, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Compose_ReturnsView_WhenBuyerOpensTradeOnSellerListing()
    {
        var buyer = new User { Username = "buy", Email = "b@t.c", CreatedAt = DateTime.UtcNow };
        var seller = new User { Username = "sel", Email = "s@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(buyer, seller);
        await _context.SaveChangesAsync();

        var subject = NewExchangeListing(seller.Id, "Subject");
        _context.Listings.Add(subject);
        await _context.SaveChangesAsync();

        SetupHttpContext(buyer.Id);
        var result = await _controller.Compose(subject.Id, null, null);

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_CreatesProposal_AndNotifiesReceiver()
    {
        var buyer = new User { Username = "buy", Email = "b@t.c", CreatedAt = DateTime.UtcNow };
        var seller = new User { Username = "sel", Email = "s@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(buyer, seller);
        await _context.SaveChangesAsync();

        var subject = NewExchangeListing(seller.Id, "Subject");
        _context.Listings.Add(subject);
        await _context.SaveChangesAsync();

        SetupHttpContext(buyer.Id);
        var redirect = await _controller.Create(
            subject.Id,
            initiatorListingIds: new List<int>(),
            receiverListingIds: new List<int> { subject.Id },
            initiatorCash: 0,
            receiverCash: 0,
            editTradeProposalId: null,
            parentTradeProposalId: null);

        redirect.Should().BeOfType<RedirectToActionResult>();
        (await _context.TradeProposals.CountAsync()).Should().Be(1);
        _notifications.Verify(
            n => n.NotifyTradeProposalAsync(seller.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Details_ReturnsForbid_WhenUserIsNotPartOfProposal()
    {
        var a = new User { Username = "a", Email = "a@t.c", CreatedAt = DateTime.UtcNow };
        var b = new User { Username = "b", Email = "b@t.c", CreatedAt = DateTime.UtcNow };
        var c = new User { Username = "c", Email = "c@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(a, b, c);
        await _context.SaveChangesAsync();

        var subject = NewExchangeListing(b.Id, "S");
        _context.Listings.Add(subject);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var p = new TradeProposal
        {
            InitiatorUserId = a.Id,
            ReceiverUserId = b.Id,
            SubjectListingId = subject.Id,
            Status = TradeProposalStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            LastModifiedAt = now
        };
        _context.TradeProposals.Add(p);
        await _context.SaveChangesAsync();

        SetupHttpContext(c.Id);
        var result = await _controller.Details(p.Id);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Accept_ReducesStock_WhenReceiverAccepts()
    {
        var initiator = new User { Username = "i", Email = "i@t.c", CreatedAt = DateTime.UtcNow };
        var receiver = new User { Username = "r", Email = "r@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(initiator, receiver);
        await _context.SaveChangesAsync();

        var initiatorListing = NewExchangeListing(initiator.Id, "Li");
        var subject = NewExchangeListing(receiver.Id, "Sub");
        _context.Listings.AddRange(initiatorListing, subject);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var p = new TradeProposal
        {
            InitiatorUserId = initiator.Id,
            ReceiverUserId = receiver.Id,
            SubjectListingId = subject.Id,
            Status = TradeProposalStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            LastModifiedAt = now,
            Items =
            {
                new TradeProposalItem { Side = TradeProposalSide.Initiator, ListingId = initiatorListing.Id, Quantity = 1 },
                new TradeProposalItem { Side = TradeProposalSide.Receiver, ListingId = subject.Id, Quantity = 1 }
            }
        };
        _context.TradeProposals.Add(p);
        await _context.SaveChangesAsync();

        SetupHttpContext(receiver.Id);
        var result = await _controller.Accept(p.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        await _context.Entry(initiatorListing).ReloadAsync();
        await _context.Entry(subject).ReloadAsync();
        initiatorListing.StockQuantity.Should().Be(0);
        subject.StockQuantity.Should().Be(0);
        (await _context.TradeProposals.AsNoTracking().FirstAsync(x => x.Id == p.Id)).Status
            .Should().Be(TradeProposalStatus.Accepted);
    }

    [Fact]
    public async Task Reject_SetsStatusRejected_WhenReceiverRejects()
    {
        var i = new User { Username = "i", Email = "i@t.c", CreatedAt = DateTime.UtcNow };
        var r = new User { Username = "r", Email = "r@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(i, r);
        await _context.SaveChangesAsync();

        var subject = NewExchangeListing(r.Id, "S");
        _context.Listings.Add(subject);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var p = new TradeProposal
        {
            InitiatorUserId = i.Id,
            ReceiverUserId = r.Id,
            SubjectListingId = subject.Id,
            Status = TradeProposalStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            LastModifiedAt = now
        };
        _context.TradeProposals.Add(p);
        await _context.SaveChangesAsync();

        SetupHttpContext(r.Id);
        var result = await _controller.Reject(p.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        (await _context.TradeProposals.AsNoTracking().FirstAsync(x => x.Id == p.Id)).Status
            .Should().Be(TradeProposalStatus.Rejected);
    }

    [Fact]
    public async Task Cancel_SetsStatusCancelled_WhenInitiatorCancels()
    {
        var i = new User { Username = "i", Email = "i@t.c", CreatedAt = DateTime.UtcNow };
        var r = new User { Username = "r", Email = "r@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(i, r);
        await _context.SaveChangesAsync();

        var subject = NewExchangeListing(r.Id, "S");
        _context.Listings.Add(subject);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var p = new TradeProposal
        {
            InitiatorUserId = i.Id,
            ReceiverUserId = r.Id,
            SubjectListingId = subject.Id,
            Status = TradeProposalStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            LastModifiedAt = now
        };
        _context.TradeProposals.Add(p);
        await _context.SaveChangesAsync();

        SetupHttpContext(i.Id);
        var result = await _controller.Cancel(p.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        (await _context.TradeProposals.AsNoTracking().FirstAsync(x => x.Id == p.Id)).Status
            .Should().Be(TradeProposalStatus.Cancelled);
    }

    [Fact]
    public async Task Index_ReturnsView_WithEmptyListsWhenNoProposals()
    {
        var u = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        SetupHttpContext(u.Id);

        var result = await _controller.Index();

        result.Should().BeOfType<ViewResult>();
    }
}
