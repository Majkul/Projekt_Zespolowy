using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using Xunit;

namespace ProjektZespolowyGr3.Tests.Models
{
    public class MyDBContextQueryFilterTests
    {
        [Theory]
        [InlineData(typeof(ReviewPhoto))]
        [InlineData(typeof(TradeProposalHistoryEntry))]
        [InlineData(typeof(TradeProposalItem))]
        public void RequiredDependentsOfFilteredEntities_ShouldHaveMatchingQueryFilters(Type entityType)
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new MyDBContext(options);

            var filter = context.Model.FindEntityType(entityType)?.GetQueryFilter();

            filter.Should().NotBeNull($"{entityType.Name} depends on an entity with a global query filter");
        }

        [Fact]
        public void ModelCreation_ShouldNotEmitRequiredNavigationQueryFilterWarnings()
        {
            var options = new DbContextOptionsBuilder<MyDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Throw(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options;

            using var context = new MyDBContext(options);

            var entityTypes = context.Model.GetEntityTypes();

            entityTypes.Should().NotBeEmpty();
        }
    }
}
