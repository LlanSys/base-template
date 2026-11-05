using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BT.SharedKernel.Features.Banking.Customers.Dtos;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using BT.SharedKernel.Dtos.Common;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using BT.Domain.Features.HR.Employees.Entities;
using BT.Persistence.Features.HR.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BT.Tests.Integration.ApiTests;

public class TenantIsolationApiTests : IClassFixture<TestFixtures.BaseTemplateWebApplicationFactory>
{
    private readonly TestFixtures.BaseTemplateWebApplicationFactory _factory;

    public TenantIsolationApiTests(TestFixtures.BaseTemplateWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CrossTenantDataAccess_ShouldReturnNotFound()
    {
        // Arrange
        var tenantA_Id = Guid.CreateVersion7();
        var tenantB_Id = Guid.CreateVersion7();
        var rmId = Guid.CreateVersion7();
        var userA = await SeedIdentityAsync(tenantA_Id, "customers.create", "customers.view");
        var userB = await SeedIdentityAsync(tenantB_Id, "customers.view");

        using (var scope = _factory.Services.CreateScope())
        {
            // Employee belongs to HrDBContext — not BankingDBContext
            var hrContext = scope.ServiceProvider.GetRequiredService<HrDBContext>();
            var employee = Employee.Create($"RM-{Guid.NewGuid().ToString()[..5]}", "rm@example.com", "RM", "User", "ID-123", "KE", "700000000", "+254700000000", Guid.NewGuid(), null, "Test");
            employee.GetType().GetProperty("Id")?.SetValue(employee, rmId);
            employee.GetType().GetProperty("TenantId")?.SetValue(employee, tenantA_Id);
            hrContext.Set<Employee>().Add(employee);
            await hrContext.SaveChangesAsync();
        }

        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(TestFixtures.TestAuthHandler.AuthenticationScheme);
        clientA.DefaultRequestHeaders.Add("X-Test-UserId", userA);
        clientA.DefaultRequestHeaders.Add("X-Test-TenantId", tenantA_Id.ToString());
        clientA.DefaultRequestHeaders.Add("X-Test-Permissions", "customers.create,customers.view");

        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(TestFixtures.TestAuthHandler.AuthenticationScheme);
        clientB.DefaultRequestHeaders.Add("X-Test-UserId", userB);
        clientB.DefaultRequestHeaders.Add("X-Test-TenantId", tenantB_Id.ToString());
        clientB.DefaultRequestHeaders.Add("X-Test-Permissions", "customers.view");

        // Act 1: Tenant A creates a customer
        var createRequest = new CreateCustomerRequest(
            Type: BT.Domain.Features.Banking.Customers.Enums.CustomerType.Individual.ToString(),
            SegmentType: BT.Domain.Features.Banking.Customers.Enums.SegmentType.Retail.ToString(),
            SubSegmentType: BT.Domain.Features.Banking.Customers.Enums.SubSegmentType.Local.ToString(),
            Classification: "Gold",
            CompanyName: "Tenant A Company",
            LineOfBusiness: BT.Domain.Features.Banking.Customers.Enums.LineOfBusiness.Agriculture.ToString(),
            LineOfBusinessMoreInfo: "Crop Farming",
            NatureOfBusiness: "Farming",
            IdentificationType: BT.Domain.Features.Banking.Customers.Enums.IdentificationType.CertificateOfIncorporation.ToString(),
            RegistrationNumber: "REG-123",
            DateOfRegistration: DateTimeOffset.UtcNow.AddYears(-5),
            RegisteredAt: "City",
            RegisteredOffice: "Office A",
            BusinessStartedYear: 2020,
            NumberOfEmployees: 10,
            Comments: null,
            Website: null,
            TINNumber: null,
            RelationshipManagerId: rmId, // Will set to existing employee ID
            OpenedOn: DateTimeOffset.UtcNow,
            ResidentialAddress: "123 Main St",
            Country: "US",
            Region: "NY",
            Ward: "Ward1",
            District: "D1",
            BusinessAddress: null,
            OfficeAddress: null,
            MailingAddress: null,
            Street: null,
            ZipCode: null,
            PhoneHome: null,
            PhoneWork: null,
            Mobile: "555-0100",
            FaxNo: null,
            LandMark: null,
            Email: "tenantA.customer@example.com",
            CanSendGreetings: true,
            CanSendAssociateSpecialOffer: false,
            CanSendOurSpecialOffers: false,
            StatementOnline: true,
            MobileAlert: true
        );

        var response = await clientA.PostAsJsonAsync("/api/v1/banking/customers", createRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var exception = new Exception($"Creation failed with {response.StatusCode}: {content}");
            throw exception;
        }

        // Verify the response
        response.EnsureSuccessStatusCode();
        var createdCustomer = await response.Content.ReadFromJsonAsync<AppResponse<CustomerResponse>>();

        Assert.NotNull(createdCustomer);
        Assert.True(createdCustomer.IsSuccess);
        var customerId = createdCustomer.Data!.Id;

        // Act 2: Tenant A gets the customer
        var getResponseA = await clientA.GetAsync($"/api/v1/banking/customers/{customerId}");
        getResponseA.EnsureSuccessStatusCode();

        // Act 3: Tenant B tries to get Tenant A's customer
        var getResponseB = await clientB.GetAsync($"/api/v1/banking/customers/{customerId}");

        // Assert: Tenant B should get a 404 because of global query filters
        Assert.Equal(HttpStatusCode.NotFound, getResponseB.StatusCode);
    }

    [Fact]
    public async Task Custom_role_access_and_module_revocation_are_enforced_by_the_api()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = await SeedIdentityAsync(tenantId);

        using (var scope = _factory.Services.CreateScope())
        {
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            var role = new AppRole { TenantId = tenantId, Name = "Accounts Supervisor", NormalizedName = "ACCOUNTS SUPERVISOR" };
            iam.Roles.Add(role);
            iam.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = role.Id });
            iam.RoleClaims.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = "customers.view" });
            await iam.SaveChangesAsync();
        }

        using var client = Client(userId, tenantId);
        var url = $"/api/v1/banking/customers/{Guid.CreateVersion7()}";
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var control = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            (await control.TenantModules.SingleAsync(grant => grant.TenantId == tenantId && grant.ModuleKey == "Banking")).IsActive = false;
            await control.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Token_tenant_mismatch_cannot_read_a_resource()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = await SeedIdentityAsync(tenantId, "customers.view");
        using var client = Client(userId, Guid.CreateVersion7());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/banking/customers/{Guid.CreateVersion7()}")).StatusCode);
    }

    [Fact]
    public async Task Permission_delegation_requires_vendor_approval_even_when_the_actor_owns_the_permission()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = await SeedIdentityAsync(tenantId, "customers.view", "roles.manage_permissions");
        var roleId = Guid.CreateVersion7().ToString();

        using (var scope = _factory.Services.CreateScope())
        {
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            iam.Roles.Add(new AppRole { Id = roleId, TenantId = tenantId, Name = "Reader", NormalizedName = "READER" });
            (await iam.Permissions.SingleAsync(permission => permission.Key == "customers.view")).IsTenantDelegatable = false;
            await iam.SaveChangesAsync();
        }

        using var client = Client(userId, tenantId);
        var url = $"/api/v1/iam/admin/roles/{roleId}/permissions";
        var request = new SharedKernel.Features.IAM.Permissions.Dtos.UpdateRolePermissionsRequest(["customers.view"]);

        try
        {
            using var denied = await client.PutAsJsonAsync(url, request);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Contains("delegated access", await denied.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            using (var scope = _factory.Services.CreateScope())
            {
                var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
                Assert.False(await iam.RoleClaims.AnyAsync(claim => claim.RoleId == roleId));
                (await iam.Permissions.SingleAsync(permission => permission.Key == "customers.view")).IsTenantDelegatable = true;
                await iam.SaveChangesAsync();
            }

            using var granted = await client.PutAsJsonAsync(url, request);
            Assert.True(granted.StatusCode == HttpStatusCode.OK, await granted.Content.ReadAsStringAsync());
            using var verification = _factory.Services.CreateScope();
            Assert.True(await verification.ServiceProvider.GetRequiredService<IamDBContext>().RoleClaims
                .AnyAsync(claim => claim.RoleId == roleId && claim.ClaimValue == "customers.view"));
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            (await iam.Permissions.SingleAsync(permission => permission.Key == "customers.view")).IsTenantDelegatable = true;
            await iam.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Read_only_platform_staff_can_view_tenants_and_modules_but_cannot_mutate_them()
    {
        var (userId, tenantId) = await SeedPlatformAsync("controlplane.view_tenants", "modules.view");
        using var client = Client(userId, tenantId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/control-plane/tenants")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/control-plane/modules")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/control-plane/stamps")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/v1/control-plane/tenants/{tenantId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/v1/control-plane/tenants/{tenantId}/suspend", null)).StatusCode);
        var tenantCreation = await client.PostAsJsonAsync(
            "/api/v1/control-plane/tenants",
            new
            {
            });

        Assert.Equal(HttpStatusCode.Forbidden, tenantCreation.StatusCode);

        var moduleCreation = await client.PostAsJsonAsync(
            "/api/v1/control-plane/modules",
            new
            {
            });

        Assert.Equal(HttpStatusCode.Forbidden, moduleCreation.StatusCode);

        var dashboard = await client.GetFromJsonAsync<AppResponse<SharedKernel.Features.ControlPlane.Tenants.Dtos.PlatformDashboardResponse>>("/api/v1/control-plane/tenants/dashboard");
        Assert.NotNull(dashboard?.Data);
        Assert.Null(dashboard.Data.ActiveStamps);
        Assert.Null(dashboard.Data.InvitationsPending);
    }

    [Fact]
    public async Task Legacy_control_plane_permission_alone_does_not_grant_new_operations()
    {
        var (userId, tenantId) = await SeedPlatformAsync("controlplane.manage");
        using var client = Client(userId, tenantId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/control-plane/tenants")).StatusCode);
    }

    [Fact]
    public async Task Platform_administrator_cannot_recreate_the_retired_permission()
    {
        var (userId, tenantId) = await SeedPlatformAsync("permissions.manage");
        using var client = Client(userId, tenantId);
        var request = new CreatePermissionRequest { Context = "ControlPlane", Resource = "controlplane", Action = "manage" };

        var response = await client.PostAsJsonAsync("/api/v1/iam/admin/permissions", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("has been retired", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shipped_and_referenced_permissions_cannot_be_retired_but_unused_custom_permissions_can()
    {
        var (userId, tenantId) = await SeedPlatformAsync("permissions.manage");
        Guid shippedId, customId;
        string roleId;

        using (var scope = _factory.Services.CreateScope())
        {
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            shippedId = await iam.Permissions.Where(permission => permission.Key == "customers.view").Select(permission => permission.Id).SingleAsync();
            var custom = BT.Domain.Features.IAM.Permissions.Entities.Permission.Create(null, "IAM", "unused_custom_" + Guid.CreateVersion7().ToString("N"), "view", "Custom definition", userId);
            iam.Permissions.Add(custom);
            customId = custom.Id;
            var role = new AppRole { Name = "Referenced role " + customId, NormalizedName = "REFERENCED " + customId, IsPlatformRole = true };
            iam.Roles.Add(role);
            roleId = role.Id;
            iam.RoleClaims.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = custom.Key });
            await iam.SaveChangesAsync();
        }

        using var client = Client(userId, tenantId);
        var shipped = await client.DeleteAsync($"/api/v1/iam/admin/permissions/{shippedId}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, shipped.StatusCode);
        Assert.Contains("shipped functionality", await shipped.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.DeleteAsync($"/api/v1/iam/admin/permissions/{customId}")).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            iam.RoleClaims.RemoveRange(await iam.RoleClaims.Where(claim => claim.RoleId == roleId).ToListAsync());
            await iam.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/v1/iam/admin/permissions/{customId}")).StatusCode);
    }

    [Fact]
    public async Task Tenant_role_relationship_endpoint_cannot_enumerate_another_tenants_users()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var actor = await SeedIdentityAsync(tenantA, "roles.view");
        await SeedIdentityAsync(tenantB);
        var role = new AppRole { TenantId = tenantB, Name = "Foreign role", NormalizedName = "FOREIGN ROLE" };

        using (var scope = _factory.Services.CreateScope())
        {
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            iam.Roles.Add(role);
            await iam.SaveChangesAsync();
        }

        using var client = Client(actor, tenantA);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/iam/admin/roles/{role.Id}/assigned-users")).StatusCode);
    }

    [Fact]
    public async Task Assigned_user_pages_exclude_foreign_tenant_assignments_and_return_the_full_result()
    {
        var tenantId = Guid.CreateVersion7();
        var actorId = await SeedIdentityAsync(tenantId, "roles.view");
        var foreignId = await SeedIdentityAsync(Guid.CreateVersion7());
        var role = new AppRole { TenantId = tenantId, Name = "Paged readers " + tenantId, NormalizedName = "PAGED READERS " + tenantId };

        using (var scope = _factory.Services.CreateScope())
        {
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            iam.Roles.Add(role);

            for (var index = 0; index < 51; index++)
            {
                var id = Guid.CreateVersion7().ToString();
                iam.Users.Add(new AppUser { Id = id, TenantId = tenantId, UserName = id, NormalizedUserName = id.ToUpperInvariant(), Email = id + "@example.test", IsActive = true });
                iam.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = id });
            }

            iam.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = foreignId });
            await iam.SaveChangesAsync();
        }

        using var client = Client(actorId, tenantId);
        var url = $"/api/v1/iam/admin/roles/{role.Id}/assigned-users";
        var first = await client.GetFromJsonAsync<AppResponse<SharedKernel.Features.IAM.Users.Dtos.AssignedUsersPage>>(url);
        Assert.Equal(50, first!.Data!.Items.Count);
        Assert.NotNull(first.Data.NextCursor);
        var second = await client.GetFromJsonAsync<AppResponse<SharedKernel.Features.IAM.Users.Dtos.AssignedUsersPage>>(url + "?cursor=" + Uri.EscapeDataString(first.Data.NextCursor));
        Assert.Single(second!.Data!.Items);
        Assert.Null(second.Data.NextCursor);
        var ids = first.Data.Items.Concat(second.Data.Items).Select(user => user.Id).ToArray();
        Assert.Equal(51, ids.Distinct().Count());
        Assert.DoesNotContain(foreignId, ids);
    }

    private async Task<(string UserId, Guid TenantId)> SeedPlatformAsync(params string[] permissions)
    {
        var tenantId = Guid.CreateVersion7();
        var userId = await SeedIdentityAsync(tenantId, permissions);
        using var scope = _factory.Services.CreateScope();
        var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
        var user = await iam.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == userId);
        user.IsPlatformStaff = true;
        user.TenantId = Guid.Empty;
        await iam.SaveChangesAsync();
        return (userId, tenantId);
    }

    [Fact]
    public async Task Onboarding_completion_cannot_claim_mfa_proof_from_the_request_or_only_an_enrollment_flag()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = await SeedIdentityAsync(tenantId);

        using (var scope = _factory.Services.CreateScope())
        {
            var control = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            var user = await iam.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == userId);
            var invitation = new TenantAdministratorInvitation(TimeProvider.System.GetUtcNow())
            {
                TenantId = tenantId,
                Email = user.Email!,
                NormalizedEmail = user.Email!.ToUpperInvariant(),
                FirstName = "Pending",
                LastName = "Administrator",
                CreatedBy = userId
            };

            invitation.Issue(BT.Infrastructure.Security.SingleUseToken.Hash(BT.Infrastructure.Security.SingleUseToken.Create()), DateTimeOffset.UtcNow.AddHours(1));
            Assert.True(invitation.Claim(userId, DateTimeOffset.UtcNow));
            control.AdministratorInvitations.Add(invitation);
            user.BeginOnboarding(invitation.Id);
            await control.SaveChangesAsync();
            await iam.SaveChangesAsync();
        }

        using var client = Client(userId, tenantId);
        var response = await client.PostAsJsonAsync(
            "/api/v1/iam/auth/onboarding/complete?mfaVerified=true",
            new
            {
                mfaVerified = true
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var verification = _factory.Services.CreateScope();
        var verifiedIam = verification.ServiceProvider.GetRequiredService<IamDBContext>();
        Assert.Empty(await verifiedIam.UserRoles.Where(role => role.UserId == userId).ToListAsync());
        Assert.True((await verifiedIam.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == userId)).RequiresOnboarding);
    }

    private HttpClient Client(string userId, Guid tenantId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(TestFixtures.TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-TenantId", tenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "customers.view,controlplane.manage");
        return client;
    }

    private async Task<string> SeedIdentityAsync(Guid tenantId, params string[] permissions)
    {
        using var scope = _factory.Services.CreateScope();
        var control = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
        var userId = Guid.CreateVersion7().ToString();
        var stampId = await control.DeploymentStamps.Select(stamp => stamp.Id).FirstAsync();
        control.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Identifier = tenantId.ToString(),
            DisplayName = "Test organization",
            HostName = tenantId + ".example.test",
            Status = TenantStatus.Active,
            DeploymentStampId = stampId,
            CreatedBy = userId
        });

        control.TenantModules.Add(new TenantModule { TenantId = tenantId, ModuleKey = "Banking", CreatedBy = userId });
        await control.SaveChangesAsync();
        var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
        iam.Users.Add(new AppUser { Id = userId, TenantId = tenantId, UserName = userId, NormalizedUserName = userId.ToUpperInvariant(), Email = userId + "@example.test", IsActive = true, TwoFactorEnabled = true });
        iam.UserClaims.AddRange(permissions.Select(key => new IdentityUserClaim<string> { UserId = userId, ClaimType = "permission", ClaimValue = key }));
        await iam.SaveChangesAsync();
        return userId;
    }
}
