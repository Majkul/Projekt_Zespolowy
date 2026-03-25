using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjektZespolowyGr3.Controllers.User;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Controllers;

public class PaymentControllerTests : IDisposable
{
    private readonly MyDBContext _context;
    private readonly Mock<IHttpClientFactory> _http = new();
    private readonly Mock<IPayuOrderSyncService> _payu = new();
    private readonly PaymentController _controller;

    public PaymentControllerTests()
    {
        var options = new DbContextOptionsBuilder<MyDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MyDBContext(options);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PayU:BaseUrl"] = "https://test.payu.local",
            ["PayU:ClientId"] = "id",
            ["PayU:ClientSecret"] = "secret",
            ["PayU:MerchantPosId"] = "pos"
        }).Build();
        _payu.Setup(p => p.TryFinalizeOrderFromPayuApiAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _payu.Setup(p => p.EnsureListingPurchasedNotificationIfNeededAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _controller = new PaymentController(
            _context,
            _http.Object,
            config,
            _payu.Object,
            NullLogger<PaymentController>.Instance);
    }

    public void Dispose() => _context.Dispose();

    private void SetupBuyer(int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "buyer"),
            new(ClaimTypes.Email, "b@test.com")
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    [Fact]
    public async Task Buy_ReturnsBadRequest_WhenListingNotFound()
    {
        var u = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        SetupBuyer(u.Id);

        var result = await _controller.Buy(999, 1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Buy_ReturnsBadRequest_WhenBuyerIsSeller()
    {
        var u = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        var listing = new Listing
        {
            Title = "Sale item",
            SellerId = u.Id,
            Type = ListingType.Sale,
            Price = 100,
            StockQuantity = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupBuyer(u.Id);

        var result = await _controller.Buy(listing.Id, 1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Buy_ReturnsBadRequest_WhenNotSaleListing()
    {
        var seller = new User { Username = "s", Email = "s@t.c", CreatedAt = DateTime.UtcNow };
        var buyer = new User { Username = "b", Email = "b@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(seller, buyer);
        await _context.SaveChangesAsync();
        var listing = new Listing
        {
            Title = "Trade only",
            SellerId = seller.Id,
            Type = ListingType.Trade,
            Price = 100,
            StockQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();
        SetupBuyer(buyer.Id);

        var result = await _controller.Buy(listing.Id, 1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Success_ReturnsNotFound_WhenOrderMissing()
    {
        var u = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        SetupBuyer(u.Id);

        var result = await _controller.Success(9999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
