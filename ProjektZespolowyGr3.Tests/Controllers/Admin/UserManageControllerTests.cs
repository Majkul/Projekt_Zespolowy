using System.Security.Claims;
using DomPogrzebowyProjekt.Controllers.Admin;
using DomPogrzebowyProjekt.Models.ViewModels;
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

    private static User CreateUser(string username, bool isAdmin = false)
    {
        return new User
        {
            Username = username,
            Email = $"{username}@example.com",
            IsAdmin = isAdmin,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<User> GetUserModel(IActionResult result)
    {
        return result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeAssignableTo<List<User>>().Subject;
    }

    private static void ShouldRequireAntiForgeryToken(string actionName, params Type[] parameterTypes)
    {
        var method = typeof(UserManageController).GetMethod(actionName, parameterTypes);

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: true)
            .Should().NotBeEmpty();
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

    [Fact]
    public async Task Index_UsersTab_UsesPageSizeAsHardLimitWithoutPagination()
    {
        for (var i = 1; i <= 30; i++)
        {
            _context.Users.Add(CreateUser($"user{i:00}"));
        }
        await _context.SaveChangesAsync();
        SetupAdmin();

        var firstPage = GetUserModel(await _controller.Index(tab: "Users", pageSize: 10, pageNumber: 1));
        var secondPage = GetUserModel(await _controller.Index(tab: "Users", pageSize: 10, pageNumber: 2));

        firstPage.Should().HaveCount(10);
        secondPage.Select(x => x.Id).Should().Equal(firstPage.Select(x => x.Id));
    }

    [Fact]
    public async Task Index_FiltersUsersAndAdminsBySelectedTab()
    {
        _context.Users.AddRange(
            CreateUser("regular1"),
            CreateUser("regular2"),
            CreateUser("admin1", isAdmin: true),
            CreateUser("admin2", isAdmin: true));
        await _context.SaveChangesAsync();
        SetupAdmin();

        var users = GetUserModel(await _controller.Index(tab: "Users", pageSize: 10));
        var admins = GetUserModel(await _controller.Index(tab: "Admins", pageSize: 10));

        users.Should().OnlyContain(u => !u.IsAdmin);
        users.Should().HaveCount(2);
        admins.Should().OnlyContain(u => u.IsAdmin);
        admins.Should().HaveCount(2);
    }

    [Fact]
    public void PostActions_RequireAntiForgeryToken()
    {
        ShouldRequireAntiForgeryToken(nameof(UserManageController.EditUser), typeof(int), typeof(EditUserViewModel));
        ShouldRequireAntiForgeryToken(nameof(UserManageController.DeleteUser), typeof(int));
    }
}
