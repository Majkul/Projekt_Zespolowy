using System.Security.Claims;
using DomPogrzebowyProjekt.Controllers.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Controllers.Admin;

public class UserManageControllerTests : IDisposable
{
    private readonly MyDBContext _context;
    private readonly UserManageController _controller;

    public UserManageControllerTests()
    {
        var options = new DbContextOptionsBuilder<MyDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MyDBContext(options);
        _controller = new UserManageController(_context);
    }

    public void Dispose() => _context.Dispose();

    private void SetupAdmin()
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Admin") };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    [Fact]
    public async Task Index_ReturnsView_ForUsersTab()
    {
        _context.Users.Add(new User { Username = "u1", Email = "a@b.c", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        SetupAdmin();

        var result = await _controller.Index("Users");

        result.Should().BeOfType<ViewResult>();
    }
}
