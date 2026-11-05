using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Persistence.Features.IAM.DataContext;
using BT.Tests.Integration.TestFixtures;

using Microsoft.EntityFrameworkCore;

using System;
using System.Threading.Tasks;

using Xunit;

namespace BT.Tests.Integration;

public abstract class TenantIsolationMutationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<IamDBContext> _options;
    private readonly TFixture _fixture;

    protected TenantIsolationMutationTests(TFixture fixture)
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

    [Fact]
    public async Task IgnoringQueryFilters_LeaksDataAcrossTenants()
    {
        // Arrange
        using (var initContext = new IamDBContext(_options))
        {
            await initContext.Database.EnsureCreatedAsync();
        }

        var tenantId1 = Guid.CreateVersion7();
        var tenantId2 = Guid.CreateVersion7();

        // Seed data for Tenant 1
        using (var seedContext1 = new IamDBContext(_options, new TestTenantProvider { TenantId = tenantId1 }))
        {
            var role = new AppRole { Name = "Tenant 1 role", NormalizedName = "TENANT 1 ROLE", TenantId = tenantId1 };
            seedContext1.Roles.Add(role);
            await seedContext1.SaveChangesAsync();
        }

        // Seed data for Tenant 2
        using (var seedContext2 = new IamDBContext(_options, new TestTenantProvider { TenantId = tenantId2 }))
        {
            var role = new AppRole { Name = "Tenant 2 role", NormalizedName = "TENANT 2 ROLE", TenantId = tenantId2 };
            seedContext2.Roles.Add(role);
            await seedContext2.SaveChangesAsync();
        }

        // Act & Assert
        // Tenant 1 should normally only see Tenant 1's data
        using (var testContext = new IamDBContext(_options, new TestTenantProvider { TenantId = tenantId1 }))
        {
            // Menus are a vendor-owned global catalogue; roles carry tenant isolation.
            var normallyVisible = await testContext.Roles.Select(role => role.TenantId).ToListAsync();
            Assert.Single(normallyVisible, tenant => tenant == tenantId1);
            Assert.DoesNotContain(tenantId2, normallyVisible);

            // MUTATION TEST: Deliberately bypass the global query filter
            var rawVisible = await testContext.Roles.IgnoreQueryFilters().AnyAsync(role => role.TenantId == tenantId2);

            // The test suite catches the leak. This proves the query filter is actually doing the work.
            Assert.True(rawVisible, "Ignoring query filters should expose data from other tenants.");
        }
    }

    private class TestTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; }
    }

    private class FakeTenantConnectionResolver : ITenantConnectionResolver
    {
        public Guid? CurrentTenantId { get; }
        private readonly string? _connectionString;

        public FakeTenantConnectionResolver(Guid tenantId, string? connectionString = null)
        {
            CurrentTenantId = tenantId;
            _connectionString = connectionString;
        }

        public Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_connectionString);

        public Task<string?> GetDatabaseProviderAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }
}





