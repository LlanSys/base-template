using System.Security.Claims;
using NSubstitute;
using BT.Api.Middleware;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Application.Features.IAM.Authorization;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using BT.Domain.Features.ControlPlane.Tenants.Events;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Domain.Features.IAM.Permissions.Entities;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;
using BT.Infrastructure.Features.IAM.Authorization;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Features.Shared.OrgSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using BT.Domain.Shared.Entities;
using BT.SharedKernel.Dtos.Common;
using Microsoft.Data.Sqlite;

namespace BT.Tests.Unit.SaaS;

public sealed class EffectiveAccessTests
{
    [Fact]
    public async Task Custom_role_grants_its_permissions_without_a_hardcoded_role_name()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Accounts Supervisor", "customers.view");
        var access = await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None);
        Assert.Contains("customers.view", access.Permissions);
    }

    [Fact]
    public async Task Familiar_role_name_without_a_grant_does_not_bypass_authorization()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("System Administrator");
        var access = await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None);
        Assert.Empty(access.Permissions);
    }

    [Fact]
    public async Task Permission_without_a_tenant_module_entitlement_is_denied()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Supervisor", "customers.view");
        fixture.Control.TenantModules.RemoveRange(fixture.Control.TenantModules.Where(grant => grant.TenantId == fixture.TenantId));
        await fixture.Control.SaveChangesAsync();
        Assert.DoesNotContain("customers.view", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Expired_entitlement_is_denied()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Supervisor", "customers.view");
        var grant = await fixture.Control.TenantModules.SingleAsync(grant => grant.TenantId == fixture.TenantId);
        grant.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await fixture.Control.SaveChangesAsync();
        Assert.DoesNotContain("customers.view", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Globally_disabled_module_is_denied_even_when_tenant_grant_remains()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Supervisor", "customers.view");
        (await fixture.Control.Modules.SingleAsync(module => module.Key == "Banking")).IsActive = false;
        await fixture.Control.SaveChangesAsync();
        Assert.DoesNotContain("customers.view", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Suspended_and_deleted_tenants_are_denied()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Supervisor", "customers.view");
        var tenant = await fixture.Control.Tenants.SingleAsync(tenant => tenant.Id == fixture.TenantId);
        tenant.Status = TenantStatus.Suspended;
        await fixture.Control.SaveChangesAsync();
        Assert.Empty((await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
        tenant.Status = TenantStatus.Active;
        tenant.MarkAsDeleted(fixture.User.Id, TimeProvider.System.GetUtcNow());
        await fixture.Control.SaveChangesAsync();
        Assert.Empty((await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Revoked_grant_stops_working_on_the_next_request()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Supervisor", "customers.view");
        Assert.Contains("customers.view", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
        fixture.Iam.RoleClaims.RemoveRange(fixture.Iam.RoleClaims.Where(claim => claim.ClaimValue == "customers.view"));
        await fixture.Iam.SaveChangesAsync();
        Assert.DoesNotContain("customers.view", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Tenant_cannot_gain_platform_authority_from_a_direct_permission_claim()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Iam.UserClaims.Add(new IdentityUserClaim<string> { UserId = fixture.User.Id, ClaimType = "permission", ClaimValue = "controlplane.manage" });
        await fixture.Iam.SaveChangesAsync();
        Assert.DoesNotContain("controlplane.manage", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Another_tenants_role_is_ignored_even_if_an_invalid_assignment_exists()
    {
        await using var fixture = await Fixture.CreateAsync();
        var role = await fixture.GrantAsync("Supervisor", "customers.view");
        role.TenantId = Guid.CreateVersion7();
        await fixture.Iam.SaveChangesAsync();
        Assert.DoesNotContain("customers.view", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Tenant_specific_functionality_is_denied_for_other_tenants()
    {
        await using var fixture = await Fixture.CreateAsync();
        var permission = Permission.Create(null, "Banking", "special_approvals", "approve", "Special approval", fixture.User.Id);
        permission.AvailableTenantId = Guid.CreateVersion7();
        permission.RequiredModule = "Banking";
        fixture.Iam.Permissions.Add(permission);
        await fixture.Iam.SaveChangesAsync();
        await fixture.GrantAsync("Supervisor", permission.Key);
        Assert.DoesNotContain(permission.Key, (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
        permission.AvailableTenantId = fixture.TenantId;
        await fixture.Iam.SaveChangesAsync();
        Assert.Contains(permission.Key, (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Default_roles_and_cross_tenant_users_cannot_be_edited_by_tenant_administrators()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Supervisor", "roles.edit");
        var boundary = new IamAccessBoundary(TimeProvider.System, fixture.Iam, fixture.Service());
        Assert.False(await boundary.CanEditRoleAsync(fixture.User.Id, new AppRole { IsDefault = true }, CancellationToken.None));
        Assert.False(await boundary.CanAccessUserAsync(fixture.User.Id, new AppUser { TenantId = Guid.CreateVersion7() }, CancellationToken.None));
        Assert.False(await boundary.CanGrantPermissionsAsync(fixture.User.Id, ["controlplane.manage"], CancellationToken.None));
    }

    [Fact]
    public async Task Middleware_replaces_stale_token_permissions_with_current_access()
    {
        await using var fixture = await Fixture.CreateAsync();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, fixture.User.Id),
                new Claim("tenant_id", fixture.TenantId.ToString()),
                new Claim("permission", "controlplane.manage"),
                new Claim("platform_staff", "true")
            }, "Test"))
        };

        await new EffectiveAccessMiddleware(_ => Task.CompletedTask).InvokeAsync(context, fixture.Service());
        Assert.False(context.User.HasClaim("permission", "controlplane.manage"));
        Assert.False(context.User.HasClaim("platform_staff", "true"));
    }

    [Fact]
    public async Task Middleware_rejects_a_token_for_another_tenant_even_when_the_user_has_permissions()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Accounts Supervisor", "customers.view");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, fixture.User.Id),
            new Claim("tenant_id", Guid.CreateVersion7().ToString())
        }, "Test"))
        };

        await new EffectiveAccessMiddleware(_ => Task.CompletedTask).InvokeAsync(context, fixture.Service());
        Assert.False(context.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Owning_a_permission_does_not_grant_delegation_authority()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Access administrator", "customers.view", "roles.manage_permissions");
        var permission = await fixture.Iam.Permissions.SingleAsync(permission => permission.Key == "customers.view");
        permission.IsTenantDelegatable = false;
        await fixture.Iam.SaveChangesAsync();
        var role = new AppRole { Name = "Reader", NormalizedName = "READER", TenantId = fixture.TenantId };
        fixture.Iam.Roles.Add(role);
        fixture.Iam.RoleClaims.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = permission.Key });
        await fixture.Iam.SaveChangesAsync();

        var boundary = new IamAccessBoundary(TimeProvider.System, fixture.Iam, fixture.Service());
        Assert.Contains(permission.Key, (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
        Assert.False(await boundary.CanGrantPermissionsAsync(fixture.User.Id, [permission.Key], CancellationToken.None));
        Assert.False(await boundary.CanAssignRolesAsync(fixture.User.Id, [role.Name!], CancellationToken.None));

        permission.IsTenantDelegatable = true;
        await fixture.Iam.SaveChangesAsync();
        boundary = new IamAccessBoundary(TimeProvider.System, fixture.Iam, fixture.Service());
        Assert.True(await boundary.CanGrantPermissionsAsync(fixture.User.Id, [permission.Key], CancellationToken.None));
        Assert.True(await boundary.CanAssignRolesAsync(fixture.User.Id, [role.Name!], CancellationToken.None));
        Assert.False(await boundary.CanGrantPermissionsAsync(fixture.User.Id, ["customers.edit"], CancellationToken.None));
    }

    [Fact]
    public async Task Catalog_override_cannot_remove_a_builtin_operations_module_requirement()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.GrantAsync("Supervisor", "customers.view");
        (await fixture.Iam.Permissions.SingleAsync(permission => permission.Key == "customers.view")).RequiredModule = "HR";
        fixture.Control.TenantModules.RemoveRange(fixture.Control.TenantModules.Where(grant => grant.TenantId == fixture.TenantId));
        fixture.Control.TenantModules.Add(new TenantModule { TenantId = fixture.TenantId, ModuleKey = "HR", CreatedBy = fixture.User.Id });
        await fixture.Iam.SaveChangesAsync();
        await fixture.Control.SaveChangesAsync();
        Assert.DoesNotContain("customers.view", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Theory]
    [InlineData("platform")]
    [InlineData("CONTROLPLANE")]
    public async Task Platform_context_classification_ignores_case(string context)
    {
        await using var fixture = await Fixture.CreateAsync();
        var permission = Permission.Create(null, context, "special_platform_action", "execute", "Platform operation", fixture.User.Id);
        fixture.Iam.Permissions.Add(permission);
        await fixture.Iam.SaveChangesAsync();
        await fixture.GrantAsync("Supervisor", permission.Key);
        Assert.DoesNotContain(permission.Key, (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Theory]
    [InlineData("CUSTOMERS.View", "Banking")]
    [InlineData("Employees.EDIT", "HR")]
    [InlineData("PAYMENTS.create", "Payments")]
    public void Permission_module_classification_ignores_case(string key, string expected)
        => Assert.Equal(expected, BT.SharedKernel.Features.ControlPlane.Modules.ModuleCatalog.ForPermission(key));

    [Fact]
    public async Task Default_initialization_preserves_an_existing_custom_role_with_the_same_display_name()
    {
        await using var fixture = await Fixture.CreateAsync();
        var custom = await fixture.GrantAsync("Tenant Administrator", "customers.view");
        using var services = RoleServices(fixture.Iam);
        await new PlatformCatalogInitializer(services.GetRequiredService<RoleManager<AppRole>>(), fixture.Iam).InitializeAsync(CancellationToken.None);
        Assert.False(custom.IsDefault);
        Assert.Single(await fixture.Iam.RoleClaims.Where(claim => claim.RoleId == custom.Id).ToListAsync());
        var template = await fixture.Iam.Roles.SingleAsync(role => role.DefinitionKey == "tenant-administrator");
        Assert.True(template.IsDefault);
        Assert.NotEqual(custom.Id, template.Id);
        Assert.NotEqual(custom.Name, template.Name);
        Assert.DoesNotContain("users.create", (await fixture.Service().GetAsync(fixture.User.Id, CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Role_names_are_local_to_a_tenant_but_default_names_are_reserved()
    {
        await using var fixture = await Fixture.CreateAsync();
        var custom = await fixture.GrantAsync("Accounts Supervisor");
        using var services = RoleServices(fixture.Iam);
        var manager = services.GetRequiredService<RoleManager<AppRole>>();
        var validator = new ScopedRoleValidator(fixture.Iam);
        Assert.True((await validator.ValidateAsync(manager, new AppRole { Name = custom.Name, TenantId = Guid.CreateVersion7() })).Succeeded);
        Assert.False((await validator.ValidateAsync(manager, new AppRole { Name = custom.Name, TenantId = fixture.TenantId })).Succeeded);
        Assert.False((await validator.ValidateAsync(manager, new AppRole { Name = custom.Name, IsDefault = true })).Succeeded);
    }

    [Fact]
    public async Task Administrative_audit_records_field_names_without_password_or_secret_values()
    {
        var clock = Substitute.For<TimeProvider>();
        var createdAt = new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);
        clock.GetUtcNow().Returns(createdAt);
        await using var fixture = await Fixture.CreateAsync(clock);
        var updatedAt = new DateTimeOffset(2026, 11, 5, 9, 30, 0, TimeSpan.Zero);
        clock.GetUtcNow().Returns(updatedAt);
        fixture.User.PasswordHash = "secret-password-hash-never-audit";
        await fixture.Iam.SaveChangesAsync();
        var entry = await fixture.Iam.Set<AdministrationAuditEntry>().SingleAsync(entry => entry.EntityId == fixture.User.Id && entry.Action == "Update");
        Assert.Equal(createdAt, fixture.User.CreatedAt);
        Assert.Equal(updatedAt, entry.OccurredAt);
        Assert.Contains("PasswordHash", entry.ChangedFields);
        Assert.DoesNotContain(fixture.User.PasswordHash, System.Text.Json.JsonSerializer.Serialize(entry));
    }

    [Fact]
    public async Task Removing_a_module_uses_the_same_instant_for_the_grant_and_event()
    {
        var now = new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        await using var fixture = await Fixture.CreateAsync(clock);

        var tenants = Substitute.For<ITenantRepository>();
        tenants.FindAll().Returns(fixture.Control.Tenants);
        var unitOfWork = Substitute.For<IControlPlaneUnitOfWork>();
        unitOfWork.Tenants.Returns(tenants);
        var actor = Substitute.For<ICurrentActorProvider>();
        actor.ActorId.Returns(fixture.User.Id);
        var handler = new RemoveTenantModuleCommandHandler(clock, unitOfWork,
            NullLogger<RemoveTenantModuleCommandHandler>.Instance, actor);

        var result = await handler.Handle(
            new RemoveTenantModuleCommand(fixture.TenantId, new RemoveTenantModuleRequest { ModuleKey = "Banking" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tenant = await fixture.Control.Tenants.SingleAsync(tenant => tenant.Id == fixture.TenantId);
        var grant = Assert.Single(tenant.Modules, grant => grant.ModuleKey == "Banking");
        Assert.False(grant.IsActive);
        Assert.Equal(now, grant.UpdatedAt);
        var revoked = Assert.IsType<TenantModuleRevokedDomainEvent>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(now, revoked.OccurredAt);
    }

    [Fact]
    public async Task Connecting_an_external_identity_is_audited_without_recording_the_provider_subject_value()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Iam.UserLogins.Add(new IdentityUserLogin<string> { UserId = fixture.User.Id, LoginProvider = "Google", ProviderKey = "private-provider-subject", ProviderDisplayName = "Google" });
        await fixture.Iam.SaveChangesAsync();
        var entry = await fixture.Iam.Set<AdministrationAuditEntry>().SingleAsync(entry => entry.EntityType == "IdentityUserLogin<string>" && entry.EntityId == fixture.User.Id);
        Assert.Contains("LoginProvider", entry.ChangedFields);
        Assert.DoesNotContain("private-provider-subject", System.Text.Json.JsonSerializer.Serialize(entry));
    }

    [Fact]
    public async Task Failed_identity_write_rolls_back_grants_and_their_audit_entries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<IamDBContext>().UseSqlite(connection).Options;
        await using var context = new RollbackIamDBContext(options);
        await context.Database.EnsureCreatedAsync();
        var role = new AppRole { Name = "Supervisor", NormalizedName = "SUPERVISOR", TenantId = Guid.CreateVersion7() };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        var auditCount = await context.Set<AdministrationAuditEntry>().IgnoreQueryFilters().CountAsync();
        var response = await IdentityWriteScope.RunAsync(context, async () =>
        {
            context.RoleClaims.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = "customers.view" });
            await context.SaveChangesAsync();
            return AppResponses.Failure<bool>("Assignment was rejected.");
        }, CancellationToken.None, System.Data.IsolationLevel.Serializable);

        Assert.False(response.IsSuccess);
        await using var verification = new RollbackIamDBContext(options);
        Assert.False(await verification.RoleClaims.AnyAsync());
        Assert.Equal(auditCount, await verification.Set<AdministrationAuditEntry>().IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Module_readiness_requires_the_shipped_menu_and_complete_default_role()
    {
        await using var fixture = await Fixture.CreateAsync();
        using var services = RoleServices(fixture.Iam);
        await new PlatformCatalogInitializer(services.GetRequiredService<RoleManager<AppRole>>(), fixture.Iam).InitializeAsync(CancellationToken.None);
        var readiness = new ModuleReadinessService(fixture.Iam);
        Assert.True(await readiness.IsReadyAsync("Banking", CancellationToken.None));
        var menu = await fixture.Iam.MenuItems.SingleAsync(menu => menu.Url == "/admin/customers");
        menu.Update(TimeProvider.System.GetUtcNow(), menu.ParentId, menu.DepartmentId, menu.Key, menu.Title, menu.Description, menu.Url, menu.Icon, menu.Placement,
            menu.RequiredPermissionKey, menu.RequiredModule, menu.DisplayOrder, false, fixture.User.Id);

        await fixture.Iam.SaveChangesAsync();
        Assert.False(await readiness.IsReadyAsync("Banking", CancellationToken.None));
        Assert.Contains((await readiness.GetIssuesAsync(CancellationToken.None))["Banking"], issue => issue.Contains("/admin/customers", StringComparison.Ordinal));
    }

    private static ServiceProvider RoleServices(IamDBContext context)
    {
        var services = new ServiceCollection().AddLogging().AddSingleton(context);
        services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<IamDBContext>();
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.RemoveAll<IRoleValidator<AppRole>>(services);
        services.AddScoped<IRoleValidator<AppRole>, ScopedRoleValidator>();
        return services.BuildServiceProvider();
    }

    private sealed class RollbackIamDBContext(DbContextOptions<IamDBContext> options) : IamDBContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entity in builder.Model.GetEntityTypes())
                if (entity.FindProperty("RowVersion") is { } version)
                    version.SetDefaultValueSql("X'00'");
        }
    }

    [Theory]
    [InlineData("Unknown.Setting", "anything", false)]
    [InlineData("Organization.TimeZone", "not-a-zone", false)]
    [InlineData("Organization.TimeZone", "Africa/Nairobi", true)]
    [InlineData("Organization.Culture", "en-KE", true)]
    [InlineData("Organization.Culture", "not-a-culture", false)]
    [InlineData("Organization.DateFormat", "yyyy-MM-dd", true)]
    public void Only_supported_setting_values_are_accepted(string key, string value, bool valid)
        => Assert.Equal(valid, OrganizationSettingCatalog.IsValid(key, value));

    [Fact]
    public void Organization_preferences_have_observable_formatting_effects()
    {
        var preferences = new OrganizationPreferences("Acme", "Africa/Nairobi", "en-KE", "yyyy-MM-dd");
        Assert.Equal("2026-10-30 03:00", OrganizationFormatting.FormatDate(new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero), preferences));
        Assert.Equal("1,234", OrganizationFormatting.FormatCount(1234, preferences));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Guid TenantId { get; } = Guid.CreateVersion7();
        public IamDBContext Iam { get; }
        public ControlPlaneDBContext Control { get; }
        public AppUser User { get; }

        private Fixture(TimeProvider? clock)
        {
            var tenant = new TenantProvider(TenantId);
            Iam = new IamDBContext(new DbContextOptionsBuilder<IamDBContext>().UseInMemoryDatabase(Guid.CreateVersion7().ToString()).Options, tenant, timeProvider: clock);
            Control = new ControlPlaneDBContext(new DbContextOptionsBuilder<ControlPlaneDBContext>().UseInMemoryDatabase(Guid.CreateVersion7().ToString()).Options, new Actor());
            User = new AppUser { Id = Guid.CreateVersion7().ToString(), TenantId = TenantId, UserName = "allan", IsActive = true };
        }

        public static async Task<Fixture> CreateAsync(TimeProvider? clock = null)
        {
            var fixture = new Fixture(clock);
            await fixture.Iam.Database.EnsureCreatedAsync();
            await fixture.Control.Database.EnsureCreatedAsync();
            fixture.Control.Tenants.Add(new Tenant
            {
                Id = fixture.TenantId,
                Identifier = fixture.TenantId.ToString(),
                DisplayName = "Acme",
                HostName = fixture.TenantId + ".example.test",
                Status = TenantStatus.Active,
                CreatedBy = fixture.User.Id
            });

            fixture.Control.TenantModules.Add(new TenantModule { TenantId = fixture.TenantId, ModuleKey = "Banking", CreatedBy = fixture.User.Id });
            fixture.Iam.Users.Add(fixture.User);
            await fixture.Iam.SaveChangesAsync();
            await fixture.Control.SaveChangesAsync();
            return fixture;
        }

        public EffectiveAccessService Service() => new(Iam, Control, TimeProvider.System);

        public async Task<AppRole> GrantAsync(string name, params string[] permissions)
        {
            var role = new AppRole { Name = name, NormalizedName = name.ToUpperInvariant(), TenantId = TenantId };
            Iam.Roles.Add(role);
            Iam.UserRoles.Add(new IdentityUserRole<string> { UserId = User.Id, RoleId = role.Id });

            foreach (var permission in permissions)
                Iam.RoleClaims.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = permission });
            await Iam.SaveChangesAsync();
            return role;
        }

        public async ValueTask DisposeAsync() { await Iam.DisposeAsync(); await Control.DisposeAsync(); }
        private sealed class TenantProvider(Guid tenantId) : ICurrentTenantProvider { public Guid TenantId => tenantId; }
        private sealed class Actor : ICurrentActorProvider { public string ActorId => "00000000-0000-0000-0000-000000000001"; }
    }
}
