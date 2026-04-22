using System.Security.Claims;
using DomPogrzebowyProjekt.Controllers.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Controllers.Admin;

public class ListingManageControllerTests : IDisposable
{
    private readonly MyDBContext _context;
    private readonly ListingManageController _controller;

    public ListingManageControllerTests()
    {
        var options = new DbContextOptionsBuilder<MyDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MyDBContext(options);
        var acc = new Mock<IHttpContextAccessor>();
        var fs = new Mock<IFileService>();
        _controller = new ListingManageController(_context, fs.Object, acc.Object);
    }

    public void Dispose() => _context.Dispose();

    private void SetupAdmin()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, "Admin")
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    [Fact]
    public async Task Index_ReturnsView_WhenAdmin()
    {
        var u = new User { Username = "s", Email = "s@b.c", CreatedAt = DateTime.UtcNow };
        _context.Users.Add(u);
        await _context.SaveChangesAsync();
        _context.Listings.Add(new Listing
        {
            Title = "L",
            SellerId = u.Id,
            Price = 10,
            StockQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        SetupAdmin();

        var result = await _controller.Index();

        result.Should().BeOfType<ViewResult>();
    }
}
