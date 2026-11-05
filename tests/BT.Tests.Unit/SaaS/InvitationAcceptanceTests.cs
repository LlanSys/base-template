using System.Security.Claims;
using BT.Api.Features.IAM.Users.Controllers;
using BT.Api.Middleware;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Configuration;
using BT.Infrastructure.Features.ControlPlane.Tenants.Services;
using BT.Infrastructure.Features.IAM.Users.Services;
using BT.Infrastructure.Security;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BT.Tests.Unit.SaaS;

public sealed class InvitationAcceptanceTests
{
    [Fact]
    public async Task Invitation_expires_at_the_injected_clock_boundary_without_waiting()
    {
        var issuedAt = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(issuedAt);
        await using var fixture = await Fixture.CreateAsync(clock);
        clock.GetUtcNow().Returns(issuedAt.AddHours(48).AddTicks(-1));
        Assert.True((await fixture.Acceptance.InspectAsync(fixture.Inspect(), CancellationToken.None)).IsSuccess);
        clock.GetUtcNow().Returns(issuedAt.AddHours(48));
        Assert.False((await fixture.Acceptance.InspectAsync(fixture.Inspect(), CancellationToken.None)).IsSuccess);
        Assert.False(await fixture.Iam.Users.AnyAsync());
    }

    [Fact]
    public async Task Inspection_does_not_create_an_identity_or_consume_the_invitation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var preview = await fixture.Acceptance.InspectAsync(fixture.Inspect(), CancellationToken.None);
        Assert.True(preview.IsSuccess);
        Assert.Equal("Invited organization", preview.Data.OrganizationName);
        Assert.Equal(InvitationAcceptanceStatus.Pending, fixture.Invitation.AcceptanceStatus);
        Assert.False(await fixture.Iam.Users.AnyAsync());
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("wrong-token")]
    [InlineData("wrong-tenant")]
    [InlineData("pending-kyc")]
    [InlineData("suspended")]
    [InlineData("deleted")]
    public async Task Ineligible_invitation_cannot_be_inspected_or_used_to_create_an_account(string scenario)
    {
        await using var fixture = await Fixture.CreateAsync();

        if (scenario == "expired") fixture.Invitation.Issue(SingleUseToken.Hash(fixture.Token), DateTimeOffset.UtcNow.AddMinutes(-1));

        if (scenario == "revoked") fixture.Invitation.Revoke(DateTimeOffset.UtcNow);

        if (scenario == "wrong-token") fixture.Token = SingleUseToken.Create();

        if (scenario == "wrong-tenant") fixture.Scope.TenantId = Guid.CreateVersion7();

        if (scenario == "pending-kyc") fixture.Tenant.Status = TenantStatus.PendingKYC;

        if (scenario == "suspended") fixture.Tenant.Suspend();

        if (scenario == "deleted") fixture.Tenant.MarkAsDeleted(fixture.Scope.ActorId, TimeProvider.System.GetUtcNow());
        await fixture.Control.SaveChangesAsync();
        Assert.False((await fixture.Acceptance.InspectAsync(fixture.Inspect(), CancellationToken.None)).IsSuccess);
        Assert.False((await fixture.Acceptance.AcceptPasswordAsync(fixture.Accept(), CancellationToken.None)).IsSuccess);
        Assert.False(await fixture.Iam.Users.IgnoreQueryFilters().AnyAsync());
    }

    [Fact]
    public async Task Password_acceptance_is_one_time_and_administrator_access_waits_for_mfa()
    {
        await using var fixture = await Fixture.CreateAsync();
        var response = await fixture.Acceptance.AcceptPasswordAsync(fixture.Accept(), CancellationToken.None);
        Assert.True(response.IsSuccess, response.Message);
        Assert.True(response.Data.MfaEnrollmentRequired);
        var user = await fixture.Iam.Users.SingleAsync();
        Assert.True(user.RequiresOnboarding);
        Assert.True(user.EmailConfirmed);
        Assert.Empty(await fixture.Users.GetRolesAsync(user));
        Assert.Null(fixture.Invitation.TokenHash);
        Assert.False((await fixture.Acceptance.AcceptPasswordAsync(fixture.Accept(), CancellationToken.None)).IsSuccess);
        Assert.False((await fixture.Acceptance.CompleteAsync(user.Id, CancellationToken.None)).IsSuccess);
        user.TwoFactorEnabled = true;
        await fixture.Iam.SaveChangesAsync();
        Assert.False((await fixture.Acceptance.CompleteAsync(user.Id, CancellationToken.None)).IsSuccess);
        Assert.Empty(await fixture.Users.GetRolesAsync(user));
        Assert.True((await fixture.Acceptance.CompleteAsync(user.Id, CancellationToken.None, mfaVerified: true)).IsSuccess);
        Assert.True((await fixture.Acceptance.CompleteAsync(user.Id, CancellationToken.None, mfaVerified: true)).IsSuccess);
        Assert.False(user.RequiresOnboarding);
        Assert.Equal(InvitationAcceptanceStatus.Accepted, fixture.Invitation.AcceptanceStatus);
        Assert.Single(await fixture.Iam.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task Revocation_after_password_setup_blocks_final_administrator_assignment()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.True((await fixture.Acceptance.AcceptPasswordAsync(fixture.Accept(), CancellationToken.None)).IsSuccess);
        var user = await fixture.Iam.Users.SingleAsync();
        user.TwoFactorEnabled = true;
        fixture.Invitation.Revoke(DateTimeOffset.UtcNow);
        await fixture.Control.SaveChangesAsync();
        await fixture.Iam.SaveChangesAsync();
        Assert.False((await fixture.Acceptance.CompleteAsync(user.Id, CancellationToken.None)).IsSuccess);
        Assert.True(user.RequiresOnboarding);
        Assert.Empty(await fixture.Iam.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task Suspension_during_setup_blocks_completion_until_tenant_is_active_again()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Acceptance.AcceptPasswordAsync(fixture.Accept(), CancellationToken.None);
        var user = await fixture.Iam.Users.SingleAsync();
        user.TwoFactorEnabled = true;
        await fixture.Iam.SaveChangesAsync();
        fixture.Tenant.Suspend();
        await fixture.Control.SaveChangesAsync();
        Assert.False((await fixture.Acceptance.CompleteAsync(user.Id, CancellationToken.None)).IsSuccess);
        fixture.Tenant.MarkAsActive();
        await fixture.Control.SaveChangesAsync();
        Assert.True((await fixture.Acceptance.CompleteAsync(user.Id, CancellationToken.None, mfaVerified: true)).IsSuccess);
    }

    [Fact]
    public async Task Pending_setup_is_restricted_even_if_general_mfa_enforcement_is_disabled()
    {
        await using var fixture = await Fixture.CreateAsync();
        var response = await fixture.Acceptance.AcceptPasswordAsync(fixture.Accept(), CancellationToken.None);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/shared/org-settings";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, response.Data.UserId)], "test"));
        var nextCalled = false;
        var middleware = new MfaEnrollmentMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            Options.Create(new MfaSettings { Enabled = false, EnforceEnrollment = false }), NullLogger<MfaEnrollmentMiddleware>.Instance);

        await middleware.InvokeAsync(context, fixture.Users);
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invitation_does_not_reset_an_existing_accounts_password()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = AppUser.Create(TimeProvider.System.GetUtcNow(), fixture.Scope.TenantId, null, fixture.Invitation.Email, "Existing", "User", fixture.Invitation.Email, "", fixture.Scope.ActorId);
        user.IsActive = true;
        user.EmailConfirmed = true;
        Assert.True((await fixture.Users.CreateAsync(user, "Existing-Password123!")).Succeeded);
        var response = await fixture.Acceptance.AcceptPasswordAsync(fixture.Accept(), CancellationToken.None);
        Assert.False(response.IsSuccess);
        Assert.True(await fixture.Users.CheckPasswordAsync(user, "Existing-Password123!"));
        Assert.False(user.RequiresOnboarding);
        Assert.Equal(InvitationAcceptanceStatus.Pending, fixture.Invitation.AcceptanceStatus);
    }

    [Theory]
    [InlineData("/api/v1/iam/auth/me", false)]
    [InlineData("/api/v1/iam/users/totp/setup", false)]
    [InlineData("/api/v1/iam/users/totp/verify", true)]
    public async Task A_temporary_mfa_challenge_cannot_be_used_as_a_signed_in_session(string path, bool allowed)
    {
        await using var fixture = await Fixture.CreateAsync();
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("temp_auth", "true")], "test"));
        var nextCalled = false;
        var middleware = new MfaEnrollmentMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            Options.Create(new MfaSettings { Enabled = false }), NullLogger<MfaEnrollmentMiddleware>.Instance);

        await middleware.InvokeAsync(context, fixture.Users);
        Assert.Equal(allowed, nextCalled);
    }

    [Fact]
    public void Resend_invalidates_the_previous_credential_and_revocation_can_be_reissued_explicitly()
    {
        var item = new TenantAdministratorInvitation(TimeProvider.System.GetUtcNow()) { Email = "admin@example.test", NormalizedEmail = "ADMIN@EXAMPLE.TEST", FirstName = "A", LastName = "B", CreatedBy = "actor" };
        var now = DateTimeOffset.UtcNow;
        var original = SingleUseToken.Create();
        item.Issue(SingleUseToken.Hash(original), now.AddHours(48));
        Assert.True(item.Retry(now));
        Assert.False(SingleUseToken.Matches(item.TokenHash, original));
        Assert.True(item.Revoke(now));
        Assert.False(item.TryLease(now.AddDays(1)));
        Assert.True(item.Reissue(now));
        Assert.Equal(InvitationAcceptanceStatus.Pending, item.AcceptanceStatus);
    }

    [Theory]
    [InlineData("https://acme.example.test/iam/sso-callback", true)]
    [InlineData("https://attacker.example/iam/sso-callback", false)]
    [InlineData("https://acme.example.test/iam/sso-callback?next=attacker", false)]
    [InlineData("https://acme.example.test/iam/sso-callback#fragment", false)]
    [InlineData("https://attacker@acme.example.test/iam/sso-callback", false)]
    [InlineData("http://acme.example.test/iam/sso-callback", false)]
    [InlineData("https://acme.example.test:444/iam/sso-callback", false)]
    public void External_sign_in_returns_only_to_the_approved_organization_origin_and_callback(string address, bool expected)
        => Assert.Equal(expected, SsoRedirectPolicy.IsAllowed(address, "https://platform.example.test/iam/sign-in", "acme.example.test"));

    [Theory]
    [InlineData(TenantStatus.PendingKYC)]
    [InlineData(TenantStatus.PendingProvisioning)]
    [InlineData(TenantStatus.ProvisioningFailed)]
    public async Task Suspending_and_reactivating_cannot_be_used_to_skip_kyc_or_initial_setup(TenantStatus status)
    {
        var tenant = new Tenant { Identifier = "pending", DisplayName = "Pending", HostName = "pending.example.test", CreatedBy = "test-actor", Status = status };
        var unit = Substitute.For<IControlPlaneUnitOfWork>();
        unit.Tenants.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = new BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers.SuspendTenantCommandHandler(TimeProvider.System, unit,
            NullLogger<BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers.SuspendTenantCommandHandler>.Instance, new ScopeProvider());

        Assert.False((await handler.Handle(new(tenant.Id), CancellationToken.None)).IsSuccess);
        Assert.Equal(status, tenant.Status);
        await unit.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("mfa_enrolled", "true", false)]
    [InlineData("amr", "mfa", false)]
    [InlineData("mfa_verified", "true", true)]
    public void Refresh_preserves_verified_application_mfa_without_promoting_enrollment_or_external_claims(string type, string value, bool expected)
    {
        var original = new ClaimsPrincipal(new ClaimsIdentity([new Claim(type, value)], "validated-access-token"));
        var refreshed = new List<Claim> { new("mfa_enrolled", "true") };
        BT.Application.Features.IAM.Authorization.MfaSessionClaims.PreserveVerifiedSession(original, refreshed);
        Assert.Equal(expected, refreshed.Any(claim => claim.Type == "mfa_verified" && claim.Value == "true"));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public ScopeProvider Scope { get; } = new();
        public IamDBContext Iam { get; }
        public ControlPlaneDBContext Control { get; }
        public Tenant Tenant { get; }
        public TenantAdministratorInvitation Invitation { get; }
        public string Token { get; set; } = SingleUseToken.Create();
        public UserManager<AppUser> Users { get; private set; } = null!;
        public AdministratorInvitationAcceptance Acceptance { get; private set; } = null!;
        private ServiceProvider _provider = null!;
        private Fixture(TimeProvider clock)
        {
            Iam = new IamDBContext(new DbContextOptionsBuilder<IamDBContext>().UseInMemoryDatabase(Guid.CreateVersion7().ToString()).Options, Scope, Scope);
            Control = new ControlPlaneDBContext(new DbContextOptionsBuilder<ControlPlaneDBContext>().UseInMemoryDatabase(Guid.CreateVersion7().ToString()).Options, Scope);
            Tenant = new Tenant { Id = Scope.TenantId, Identifier = Scope.TenantId.ToString(), DisplayName = "Invited organization", HostName = "acme.example.test", Status = TenantStatus.Active, CreatedBy = Scope.ActorId };
            Invitation = new TenantAdministratorInvitation(clock.GetUtcNow()) { Tenant = Tenant, TenantId = Tenant.Id, Email = "admin@example.test", NormalizedEmail = "ADMIN@EXAMPLE.TEST", FirstName = "Initial", LastName = "Administrator", CreatedBy = Scope.ActorId };
        }
        public static async Task<Fixture> CreateAsync(TimeProvider? timeProvider = null)
        {
            var clock = timeProvider ?? TimeProvider.System;
            var fixture = new Fixture(clock);
            await fixture.Iam.Database.EnsureCreatedAsync();
            await fixture.Control.Database.EnsureCreatedAsync();
            fixture.Invitation.Issue(SingleUseToken.Hash(fixture.Token), clock.GetUtcNow().AddHours(48));
            fixture.Control.AdministratorInvitations.Add(fixture.Invitation);
            await fixture.Control.SaveChangesAsync();
            fixture.Iam.Roles.Add(new AppRole { Name = "Invitation Administrators", NormalizedName = "INVITATION ADMINISTRATORS", IsActive = true, IsDefault = true, DefinitionKey = "tenant-administrator" });
            await fixture.Iam.SaveChangesAsync();
            var services = new ServiceCollection().AddLogging().AddHttpContextAccessor();
            services.AddAuthentication();
            services.AddSingleton(fixture.Iam);
            services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddSignInManager().AddEntityFrameworkStores<IamDBContext>();
            fixture._provider = services.BuildServiceProvider();
            fixture.Users = fixture._provider.GetRequiredService<UserManager<AppUser>>();
            var controlUnit = Substitute.For<IControlPlaneUnitOfWork>();
            controlUnit.CompleteAsync(Arg.Any<CancellationToken>()).Returns(call => fixture.Control.SaveChangesAsync(call.Arg<CancellationToken>()));
            var iamUnit = Substitute.For<IIamUnitOfWork>();
            var jwt = Substitute.For<IJwtService>();
            jwt.CreateTokenAsync(Arg.Any<List<Claim>>()).Returns("access-token");
            jwt.CreateRefreshToken().Returns("refresh-token");
            var claims = Substitute.For<IClaimsService>();
            claims.GetUserClaimsAsync(Arg.Any<AppUser>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(call => new List<Claim> { new(ClaimTypes.NameIdentifier, call.Arg<AppUser>().Id) });
            var session = Substitute.For<ISessionService>();
            session.CreateSessionAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => AppResponses.Success(call.Arg<Guid>()));

            var factory = new SignInResponseFactory(fixture.Users, jwt, claims, session, iamUnit, fixture._provider.GetRequiredService<IHttpContextAccessor>(), Options.Create(new JwtSettings()), Options.Create(new MfaSettings()), fixture.Control, clock);
            fixture.Acceptance = new AdministratorInvitationAcceptance(fixture.Control, fixture.Iam, controlUnit, fixture.Users,
                fixture._provider.GetRequiredService<SignInManager<AppUser>>(), fixture.Scope, factory, clock);

            return fixture;
        }
        public InspectAdministratorInvitationRequest Inspect() => new(Invitation.Id, Token);
        public AcceptAdministratorInvitationRequest Accept() => new(Invitation.Id, Token, "New-Password123!");
        public async ValueTask DisposeAsync() { await _provider.DisposeAsync(); await Control.DisposeAsync(); }
    }
    private sealed class ScopeProvider : ICurrentTenantProvider, ICurrentActorProvider
    {
        public Guid TenantId { get; set; } = Guid.CreateVersion7();
        public string ActorId => "test-actor";
    }
}
