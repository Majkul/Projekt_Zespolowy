using Xunit;
using FluentAssertions;
using System.Linq;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ProjektZespolowyGr3.Tests.Services
{
    public class HelperServiceTests : IDisposable
    {
        private readonly MyDBContext _context;
        private readonly HelperService _helperService;

        public HelperServiceTests()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MyDBContext(options);
            _helperService = new HelperService(_context);
        }

        [Fact]
        public void MakeSomeTags_ShouldCreateTags_WhenNoTagsExist()
        {
            // Act
            var count = _helperService.MakeSomeTags();

            // Assert
            count.Should().Be(5);
            _context.Tags.Count().Should().Be(5);
            _context.Tags.Any(t => t.Name == "Electronics").Should().BeTrue();
            _context.Tags.Any(t => t.Name == "Furniture").Should().BeTrue();
            _context.Tags.Any(t => t.Name == "Books").Should().BeTrue();
            _context.Tags.Any(t => t.Name == "Clothing").Should().BeTrue();
            _context.Tags.Any(t => t.Name == "Toys").Should().BeTrue();
        }

        [Fact]
        public void MakeSomeTags_ShouldNotCreateDuplicateTags_WhenTagsAlreadyExist()
        {
            // Arrange
            _context.Tags.Add(new Tag { Name = "ExistingTag" });
            _context.SaveChanges();
            var initialCount = _context.Tags.Count();

            // Act
            var count = _helperService.MakeSomeTags();

            // Assert
            // MakeSomeTags dodaje tagi tylko jeśli nie ma żadnych, więc jeśli już są tagi, nie doda więcej
            count.Should().Be(initialCount);
            _context.Tags.Count().Should().Be(initialCount);
        }

        [Fact]
        public void PopulateAvailableTags_ShouldPopulateTagsInViewModel()
        {
            // Arrange
            _context.Tags.AddRange(new[]
            {
                new Tag { Name = "Tag1" },
                new Tag { Name = "Tag2" },
                new Tag { Name = "Tag3" }
            });
            _context.SaveChanges();

            var viewModel = new CreateListingViewModel();

            // Act
            _helperService.PopulateAvailableTags(viewModel);

            // Assert
            viewModel.AvailableTags.Should().NotBeNull();
            viewModel.AvailableTags.Should().HaveCount(3);
            viewModel.AvailableTags.Should().Contain(t => t.Text == "Tag1");
            viewModel.AvailableTags.Should().Contain(t => t.Text == "Tag2");
            viewModel.AvailableTags.Should().Contain(t => t.Text == "Tag3");
        }

        [Fact]
        public void PopulateAvailableTags_ShouldOrderTagsByName()
        {
            // Arrange
            _context.Tags.AddRange(new[]
            {
                new Tag { Name = "Zebra" },
                new Tag { Name = "Apple" },
                new Tag { Name = "Banana" }
            });
            _context.SaveChanges();

            var viewModel = new CreateListingViewModel();

            // Act
            _helperService.PopulateAvailableTags(viewModel);

            // Assert
            viewModel.AvailableTags.Should().NotBeNull();
            var tagsList = viewModel.AvailableTags.ToList();
            tagsList[0].Text.Should().Be("Apple");
            tagsList[1].Text.Should().Be("Banana");
            tagsList[2].Text.Should().Be("Zebra");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

