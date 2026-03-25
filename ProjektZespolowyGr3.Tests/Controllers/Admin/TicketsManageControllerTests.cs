using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Controllers.Admin;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Controllers.Admin;

public class TicketsManageControllerTests : IDisposable
{
    private readonly MyDBContext _context;
    private readonly TicketsManageController _controller;

    public TicketsManageControllerTests()
    {
        var options = new DbContextOptionsBuilder<MyDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MyDBContext(options);
        _controller = new TicketsManageController(_context);
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
    public async Task Index_ReturnsView_WithTickets()
    {
        var admin = new User { Username = "adm", Email = "a@t.c", IsAdmin = true, CreatedAt = DateTime.UtcNow };
        var user = new User { Username = "u", Email = "u@t.c", CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(admin, user);
        await _context.SaveChangesAsync();

        _context.Tickets.Add(new Ticket
        {
            UserId = user.Id,
            Category = TicketCategory.Other_Issue,
            Status = TicketStatus.Open,
            Subject = "T",
            Description = "D",
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        SetupAdmin();
        var result = await _controller.Index();

        result.Should().BeOfType<ViewResult>();
    }
}
