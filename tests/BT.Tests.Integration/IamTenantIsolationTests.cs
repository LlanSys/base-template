using System;
using System.Threading.Tasks;

using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Persistence.Features.IAM.DataContext;
using BT.Tests.Integration.TestFixtures;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace BT.Tests.Integration;

public abstract class IamTenantIsolationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<IamDBContext> _options;
    private readonly TFixture _fixture;

    protected IamTenantIsolationTests(TFixture fixture)
    {
        _fixture = fixture;

        var builder = new DbContextOptionsBuilder<IamDBContext>();

        if (fixture is PostgreSqlDbFixture)
        {
            builder.UseNpgsql(fixture.GetConnectionString());
        }
        else if (fixture is MsSqlDbFixture)
        {
            builder.UseSqlServer(fixture.GetConnectionString());
        }

        _options = builder.Options;
    }

    private class TestTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; }
    }

    private class TestActorProvider : ICurrentActorProvider
    {
        public string ActorId => "TestActor";
    }

    [Fact]
    public async Task Query_ShouldOnlyReturnDataForCurrentTenant()
    {
        // Arrange
        var tenant1 = Guid.CreateVersion7();
        var tenant2 = Guid.CreateVersion7();

        var tenantProvider = new TestTenantProvider { TenantId = tenant1 };
        var actorProvider = new TestActorProvider();

        // Seed data acting as tenant 1
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            await context.Database.EnsureCreatedAsync();

            var menu1 = new AppRole { Name = $"menu-1-{tenant1}", NormalizedName = $"MENU-1-{tenant1}" };
            context.Roles.Add(menu1);
            await context.SaveChangesAsync();
        }

        // Seed data acting as tenant 2
        tenantProvider.TenantId = tenant2;

        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var menu2 = new AppRole { Name = $"menu-2-{tenant2}", NormalizedName = $"MENU-2-{tenant2}" };
            context.Roles.Add(menu2);
            await context.SaveChangesAsync();
        }

        // Act - Query as tenant 1
        tenantProvider.TenantId = tenant1;

        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.Roles.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Name == $"menu-1-{tenant1}");
            Assert.DoesNotContain(items, i => i.Name == $"menu-2-{tenant2}");
        }

        // Act - Query as tenant 2
        tenantProvider.TenantId = tenant2;

        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.Roles.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Name == $"menu-2-{tenant2}");
            Assert.DoesNotContain(items, i => i.Name == $"menu-1-{tenant1}");
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnDataForAllTenants()
    {
        // Arrange
        var tenant1 = Guid.CreateVersion7();
        var tenant2 = Guid.CreateVersion7();

        var tenantProvider = new TestTenantProvider { TenantId = tenant1 };
        var actorProvider = new TestActorProvider();

        // Seed data acting as tenant 1
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            await context.Database.EnsureCreatedAsync();

            var menu1 = new AppRole { Name = $"menu-3-{tenant1}", NormalizedName = $"MENU-3-{tenant1}" };
            context.Roles.Add(menu1);
            await context.SaveChangesAsync();
        }

        // Seed data acting as tenant 2
        tenantProvider.TenantId = tenant2;

        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var menu2 = new AppRole { Name = $"menu-4-{tenant2}", NormalizedName = $"MENU-4-{tenant2}" };
            context.Roles.Add(menu2);
            await context.SaveChangesAsync();
        }

        // Act - Ignore query filters
        tenantProvider.TenantId = tenant1; // Doesn't matter for this query

        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.Roles.IgnoreQueryFilters().ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Name == $"menu-3-{tenant1}");
            Assert.Contains(items, i => i.Name == $"menu-4-{tenant2}");
        }
    }
}





