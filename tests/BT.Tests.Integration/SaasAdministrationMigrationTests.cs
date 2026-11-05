using BT.Domain.Shared.Contracts.Common;
using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Infrastructure.Features.ControlPlane.Tenants.Services;
using BT.Infrastructure.Configuration;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.Notifications.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Features.IAM.Authorization;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.Shared.DataContext;
using BT.Persistence.Features.Shared.Migrations.Generators;
using BT.Tests.Integration.TestFixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MassTransit.EntityFrameworkCoreIntegration;

namespace BT.Tests.Integration;

public sealed class SaasAdministrationMigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upgrade_preserves_custom_menus_and_active_stamps_and_initializes_defaults(bool postgres)
    {
        DbFixture fixture = postgres ? new PostgreSqlDbFixture() : new MsSqlDbFixture();
        await fixture.InitializeAsync();

        try
        {
            await using ControlPlaneDBContext control = postgres
                ? new ControlPlanePostgreSqlDBContext(Options<ControlPlanePostgreSqlDBContext>(fixture, "ControlPlane"))
                : new ControlPlaneSqlServerDBContext(Options<ControlPlaneSqlServerDBContext>(fixture, "ControlPlane"));

            var scope = new TestScope();
            await using IamDBContext iam = postgres
                ? new IamPostgreSqlDBContext(Options<IamPostgreSqlDBContext>(fixture, "IAM"), scope, scope)
                : new IamSqlServerDBContext(Options<IamSqlServerDBContext>(fixture, "IAM"), scope, scope);

            await using SharedDBContext shared = postgres
                ? new SharedPostgreSqlDBContext(Options<SharedPostgreSqlDBContext>(fixture, "Shared"), scope, scope)
                : new SharedSqlServerDBContext(Options<SharedSqlServerDBContext>(fixture, "Shared"), scope, scope);

            foreach (var context in new DbContext[] { control, iam, shared })
            {
                var previous = context.Database.GetMigrations().TakeWhile(name => !name.EndsWith("_SaasAdministration", StringComparison.Ordinal)).Last();
                await CreatePreviousSchemaAsync(context, previous);
            }

            string Quote(string identifier) => postgres ? '"' + identifier + '"' : '[' + identifier + ']';
            var menuId = Guid.CreateVersion7();
            var stampId = Guid.CreateVersion7();
            var columns = new[] { "Id", "TenantId", "ParentId", "Key", "Title", "Description", "Url", "Icon", "Placement", "DisplayOrder", "IsActive", "IsDeleted", "CreatedBy", "CreatedAt" };
            var sql = "INSERT INTO " + Quote("MenuItems") + " (" + string.Join(", ", columns.Select(Quote)) + ") VALUES (" + string.Join(", ", Enumerable.Range(0, columns.Length).Select(index => "{" + index + "}")) + ")";
            await iam.Database.ExecuteSqlRawAsync(sql, new object[] { menuId, Guid.Empty, Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "custom-menu", "Custom menu", "Keep this entry", "/admin/customers", "Business", "Sidebar", 99, true, false, scope.ActorId, DateTimeOffset.UtcNow });
            columns = ["Id", "Name", "TargetResourceGroup", "IsolationTier", "CreatedBy", "CreatedAt"];

            if (postgres)
            {
                columns = [.. columns, "RowVersion"];
            }

            sql = "INSERT INTO " + Quote("DeploymentStamps") + " (" + string.Join(", ", columns.Select(Quote)) + ") VALUES (" + string.Join(", ", Enumerable.Range(0, columns.Length).Select(index => "{" + index + "}")) + ")";
            object[] stampValues = [stampId, "existing-custom-stamp", "rg-test", 0, scope.ActorId, DateTimeOffset.UtcNow];

            if (postgres)
            {
                stampValues = [.. stampValues, Array.Empty<byte>()];
            }

            await control.Database.ExecuteSqlRawAsync(sql, stampValues);

            var queuedOutboxId = Guid.CreateVersion7();
            var queuedMessageId = Guid.CreateVersion7();
            var queuedState = new OutboxState { OutboxId = queuedOutboxId, Created = DateTime.UtcNow };
            shared.Set<OutboxState>().Add(queuedState);
            shared.Entry(queuedState).Property("BusName").CurrentValue = "MassTransit.IBus";
            shared.Set<OutboxMessage>().Add(new OutboxMessage
            {
                MessageId = queuedMessageId,
                OutboxId = queuedOutboxId,
                SentTime = DateTime.UtcNow,
                ContentType = "application/json",
                MessageType = "urn:message:Certification:QueuedMessage",
                Body = "{\"preserve\":true}",
                Headers = "{}",
                Properties = "{}"
            });

            await shared.SaveChangesAsync();

            await control.Database.MigrateAsync();
            await shared.Database.MigrateAsync();
            await VerifyLegacyPermissionRetirementAsync(iam, scope.ActorId);
            Assert.Equal("{\"preserve\":true}", await shared.Set<OutboxMessage>().Where(message => message.MessageId == queuedMessageId).Select(message => message.Body).SingleAsync());
            Assert.Equal("MassTransit.IBus", await shared.Set<OutboxState>().Where(state => state.OutboxId == queuedOutboxId).Select(state => EF.Property<string>(state, "BusName")).SingleAsync());
            Assert.Null((await iam.MenuItems.SingleAsync(menu => menu.Id == menuId)).ParentId);
            Assert.True((await control.DeploymentStamps.SingleAsync(stamp => stamp.Id == stampId)).IsActive);
            Assert.Equal(3, await control.Modules.CountAsync());
            Assert.True((await iam.Permissions.SingleAsync(permission => permission.Key == "customers.view")).IsTenantDelegatable);

            var services = new ServiceCollection().AddLogging().AddHttpContextAccessor().AddSingleton(iam);
            services.AddAuthentication();
            services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddSignInManager().AddEntityFrameworkStores<IamDBContext>();
            using var provider = services.BuildServiceProvider();
            await new PlatformCatalogInitializer(provider.GetRequiredService<RoleManager<AppRole>>(), iam).InitializeAsync(CancellationToken.None);
            Assert.True(await iam.Roles.AnyAsync(role => role.DefinitionKey == "tenant-administrator" && role.IsDefault));
            Assert.True(await iam.Roles.AnyAsync(role => role.DefinitionKey == "platform-administrator" && role.IsPlatformRole));
            await VerifyInvitationRecoveryAsync(control, iam, provider, stampId, scope);

        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static async Task VerifyLegacyPermissionRetirementAsync(IamDBContext iam, string actorId)
    {
        const string legacyKey = "controlplane.manage";
        var previous = iam.Database.GetMigrations()
            .TakeWhile(name => !name.EndsWith("_RetireLegacyControlPlanePermission", StringComparison.Ordinal))
            .Last();

        await iam.GetService<IMigrator>().MigrateAsync(previous);

        Assert.True(await iam.Permissions.AnyAsync(permission => permission.Key == legacyKey));
        var tenantId = Guid.CreateVersion7();
        var role = new AppRole
        {
            Name = "Legacy Support",
            NormalizedName = "LEGACY SUPPORT",
            IsPlatformRole = true,
            TenantId = tenantId
        };

        var user = new AppUser
        {
            Id = Guid.CreateVersion7().ToString(),
            UserName = "legacy-support",
            NormalizedUserName = "LEGACY-SUPPORT",
            TenantId = tenantId,
            IsPlatformStaff = true,
            IsActive = true,
            CreatedBy = actorId
        };

        iam.Roles.Add(role);
        iam.Users.Add(user);
        iam.RoleClaims.AddRange(
            new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = legacyKey },
            new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = "controlplane.view_tenants" });

        iam.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = user.Id, ClaimType = "permission", ClaimValue = "CONTROLPLANE.MANAGE" },
            new IdentityUserClaim<string> { UserId = user.Id, ClaimType = "permission", ClaimValue = "roles.view" },
            new IdentityUserClaim<string> { UserId = user.Id, ClaimType = "example", ClaimValue = legacyKey });

        await iam.SaveChangesAsync();

        await iam.Database.MigrateAsync();
        iam.ChangeTracker.Clear();

        Assert.False(await iam.Permissions.IgnoreQueryFilters().AnyAsync(permission => permission.Key == legacyKey));
        Assert.Equal(
            "controlplane.view_tenants",
            await iam.RoleClaims
                .Where(claim => claim.RoleId == role.Id && claim.ClaimType == "permission")
                .Select(claim => claim.ClaimValue)
                .SingleAsync());

        Assert.Equal(
            "roles.view",
            await iam.UserClaims
                .Where(claim => claim.UserId == user.Id && claim.ClaimType == "permission")
                .Select(claim => claim.ClaimValue)
                .SingleAsync());

        Assert.True(await iam.UserClaims.AnyAsync(claim => claim.UserId == user.Id
            && claim.ClaimType == "example" && claim.ClaimValue == legacyKey));
    }

    private static async Task VerifyInvitationRecoveryAsync(ControlPlaneDBContext control, IamDBContext iam, ServiceProvider provider, Guid stampId, TestScope scope)
    {
        var tenantId = Guid.CreateVersion7();
        var tenant = new Tenant
        {
            Id = tenantId,
            Identifier = "onboarding-" + tenantId,
            DisplayName = "Onboarding organization",
            HostName = tenantId + ".example.test",
            DeploymentStampId = stampId,
            Status = TenantStatus.PendingKYC,
            CreatedBy = "test-actor"
        };

        var invitation = new TenantAdministratorInvitation(TimeProvider.System.GetUtcNow())
        {
            Tenant = tenant,
            TenantId = tenantId,
            Email = "administrator@example.test",
            NormalizedEmail = "ADMINISTRATOR@EXAMPLE.TEST",
            FirstName = "Initial",
            LastName = "Administrator",
            CreatedBy = "test-actor"
        };

        tenant.AdministratorInvitations.Add(invitation);
        control.Tenants.Add(tenant);
        await control.SaveChangesAsync();
        var email = Substitute.For<IEmailService>();
        email.SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>()).Returns(
            AppResponses.Failure<SendEmailResponse>("Simulated provider outage."),
            AppResponses.Success(new SendEmailResponse("test-message", DateTimeOffset.UtcNow, invitation.Email, "Invitation")));

        var provisioner = new TenantAdministratorProvisioner(
            email,
            Microsoft.Extensions.Options.Options.Create(new IamProvisioningSettings { SignInUrl = "https://application.example.test/iam/sign-in" }));

        var dispatcher = new TenantInvitationDispatcher(
            control,
            provisioner,
            NullLogger<TenantInvitationDispatcher>.Instance,
            Microsoft.Extensions.Options.Options.Create(new AdministratorInvitationSettings()),
            TimeProvider.System);

        await dispatcher.DispatchAsync(CancellationToken.None);
        Assert.False(await iam.Users.IgnoreQueryFilters().AnyAsync(user => user.TenantId == tenantId));
        tenant.ApproveKYC(TimeProvider.System.GetUtcNow(), scope.ActorId);
        tenant.MarkAsActive();
        await control.SaveChangesAsync();
        await dispatcher.DispatchAsync(CancellationToken.None);
        Assert.Equal(AdministratorInvitationStatus.Failed, invitation.Status);
        Assert.False(await iam.Users.IgnoreQueryFilters().AnyAsync(user => user.TenantId == tenantId));
        Assert.True(invitation.Retry(DateTimeOffset.UtcNow));
        await control.SaveChangesAsync();
        await dispatcher.DispatchAsync(CancellationToken.None);
        Assert.Equal(AdministratorInvitationStatus.Delivered, invitation.Status);
        Assert.False(await iam.Users.IgnoreQueryFilters().AnyAsync(user => user.TenantId == tenantId));
        Assert.NotNull(invitation.TokenHash);
        Assert.True(invitation.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal(InvitationAcceptanceStatus.Pending, invitation.AcceptanceStatus);
        await dispatcher.DispatchAsync(CancellationToken.None);
        await email.Received(2).SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>());
        Assert.DoesNotContain("Password", invitation.LastFailure ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        await VerifyAcceptanceAndExternalSignInAsync(control, iam, provider, tenant, invitation, scope);
    }

    private static async Task VerifyAcceptanceAndExternalSignInAsync(
        ControlPlaneDBContext control,
        IamDBContext iam,
        ServiceProvider provider,
        Tenant tenant,
        TenantAdministratorInvitation invitation,
        TestScope scope)
    {
        scope.TenantId = tenant.Id;
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var signIn = provider.GetRequiredService<SignInManager<AppUser>>();
        var controlUnit = Substitute.For<Domain.Features.ControlPlane.Contracts.IControlPlaneUnitOfWork>();
        controlUnit.CompleteAsync(Arg.Any<CancellationToken>()).Returns(call => control.SaveChangesAsync(call.Arg<CancellationToken>()));
        var iamUnit = Substitute.For<Domain.Features.IAM.Contracts.IIamUnitOfWork>();
        iamUnit.CompleteAsync(Arg.Any<CancellationToken>()).Returns(call => iam.SaveChangesAsync(call.Arg<CancellationToken>()));
        var jwt = Substitute.For<Application.Features.IAM.Users.Contracts.Interfaces.IJwtService>();
        jwt.CreateTokenAsync(Arg.Any<List<System.Security.Claims.Claim>>()).Returns("test-access");
        jwt.CreateTempTokenAsync(Arg.Any<List<System.Security.Claims.Claim>>(), Arg.Any<TimeSpan>()).Returns("test-mfa-challenge");
        jwt.CreateRefreshToken().Returns("test-refresh");
        var claims = Substitute.For<Application.Features.IAM.Users.Contracts.Interfaces.IClaimsService>();
        claims.GetUserClaimsAsync(Arg.Any<AppUser>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(call => new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.NameIdentifier, call.Arg<AppUser>().Id) });

        var sessions = Substitute.For<Application.Features.IAM.Users.Contracts.Interfaces.ISessionService>();
        sessions.CreateSessionAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => AppResponses.Success(call.Arg<Guid>()));

        var responses = new Infrastructure.Features.IAM.Users.Services.SignInResponseFactory(
            users,
            jwt,
            claims,
            sessions,
            iamUnit,
            provider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Microsoft.Extensions.Options.Options.Create(new JwtSettings()),
            Microsoft.Extensions.Options.Options.Create(new MfaSettings()),
            control,
            TimeProvider.System);

        var acceptance = new AdministratorInvitationAcceptance(control, iam, controlUnit, users, signIn, scope, responses, TimeProvider.System);
        var token = BT.Infrastructure.Security.SingleUseToken.Create();
        invitation.Issue(BT.Infrastructure.Security.SingleUseToken.Hash(token), DateTimeOffset.UtcNow.AddHours(48));
        await control.SaveChangesAsync();
        var preview = await acceptance.InspectAsync(new(invitation.Id, token), CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.Message);
        Assert.False(await iam.Users.AnyAsync());
        var accepted = await acceptance.AcceptPasswordAsync(new(invitation.Id, token, "Acceptance-Password123!"), CancellationToken.None);
        Assert.True(accepted.IsSuccess, accepted.Message);
        Assert.True(accepted.Data.MfaEnrollmentRequired);
        var user = await users.FindByIdAsync(accepted.Data.UserId);
        Assert.NotNull(user);
        Assert.True(user.RequiresOnboarding);
        Assert.Empty(await users.GetRolesAsync(user));
        Assert.False((await acceptance.AcceptPasswordAsync(new(invitation.Id, token, "Acceptance-Password123!"), CancellationToken.None)).IsSuccess);
        Assert.False((await acceptance.CompleteAsync(user.Id, CancellationToken.None)).IsSuccess);
        user.TwoFactorEnabled = true; // Service boundary: OTP verification is exercised separately by HTTP/browser checks.
        Assert.True((await users.UpdateAsync(user)).Succeeded);
        Assert.True((await acceptance.CompleteAsync(user.Id, CancellationToken.None, mfaVerified: true)).IsSuccess);
        Assert.True((await acceptance.CompleteAsync(user.Id, CancellationToken.None, mfaVerified: true)).IsSuccess);
        Assert.Single(await users.GetRolesAsync(user));
        Assert.Equal(InvitationAcceptanceStatus.Accepted, invitation.AcceptanceStatus);

        var links = new Infrastructure.Features.IAM.Users.Services.ExternalAccountLinkService(iam, users, signIn, iamUnit, scope, TimeProvider.System);
        Assert.False((await links.CreateAsync(user.Id, new("Google", "Wrong-Password123!"), CancellationToken.None)).IsSuccess);
        var connection = await links.CreateAsync(user.Id, new("Google", "Acceptance-Password123!"), CancellationToken.None);
        Assert.True(connection.IsSuccess, connection.Message);
        const string providerKey = "https://accounts.google.com|provider-subject";
        Assert.True((await links.ConnectAsync(connection.Data, "Google", providerKey, CancellationToken.None)).IsSuccess);
        Assert.False((await links.ConnectAsync(connection.Data, "Google", providerKey, CancellationToken.None)).IsSuccess);
        Assert.True(await users.CheckPasswordAsync(user, "Acceptance-Password123!"));
        var process = new Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers.ProcessSsoLogin(users, iam, iamUnit, scope, acceptance, TimeProvider.System);
        var issued = await process.Handle(new(user.Email!, "", "", "Google", providerKey, tenant.Id), CancellationToken.None);
        Assert.True(issued.IsSuccess, issued.Message);
        Assert.Equal(64, issued.Data.Length);
        Assert.False(await iam.Set<SsoAuthorizationCode>().AnyAsync(code => code.Hash == issued.Data));
        var exchange = new Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers.ExchangeSsoCode(iam, scope, users, responses, TimeProvider.System);
        scope.TenantId = Guid.CreateVersion7();
        Assert.False((await exchange.Handle(new(issued.Data), CancellationToken.None)).IsSuccess);
        scope.TenantId = tenant.Id;
        var signedIn = await exchange.Handle(new(issued.Data), CancellationToken.None);
        Assert.True(signedIn.IsSuccess, signedIn.Message);
        Assert.True(signedIn.Data.Requires2FA);
        Assert.False(signedIn.Data.IsAuthenticated);
        Assert.False((await exchange.Handle(new(issued.Data), CancellationToken.None)).IsSuccess);

        var external = new TenantAdministratorInvitation(TimeProvider.System.GetUtcNow())
        {
            TenantId = tenant.Id,
            Tenant = tenant,
            Email = "external@example.test",
            NormalizedEmail = "EXTERNAL@EXAMPLE.TEST",
            FirstName = "External",
            LastName = "Administrator",
            CreatedBy = scope.ActorId
        };

        var externalToken = BT.Infrastructure.Security.SingleUseToken.Create();
        external.Issue(BT.Infrastructure.Security.SingleUseToken.Hash(externalToken), DateTimeOffset.UtcNow.AddHours(48));
        control.AdministratorInvitations.Add(external);
        await control.SaveChangesAsync();
        Assert.False((await acceptance.AcceptExternalAsync(external.Id, externalToken, "Google", "other-subject", "wrong@example.test", CancellationToken.None)).IsSuccess);
        var externalAccepted = await acceptance.AcceptExternalAsync(external.Id, externalToken, "Google", "other-subject", external.Email, CancellationToken.None);
        Assert.True(externalAccepted.IsSuccess, externalAccepted.Message);
        var externalUser = await users.FindByIdAsync(externalAccepted.Data);
        Assert.NotNull(externalUser);
        Assert.Null(externalUser.PasswordHash);
        Assert.True(externalUser.RequiresOnboarding);
        Assert.Empty(await users.GetRolesAsync(externalUser));
        Assert.False((await acceptance.CompleteAsync(externalUser.Id, CancellationToken.None)).IsSuccess);

        var race = new TenantAdministratorInvitation(TimeProvider.System.GetUtcNow())
        {
            TenantId = tenant.Id,
            Tenant = tenant,
            Email = "race@example.test",
            NormalizedEmail = "RACE@EXAMPLE.TEST",
            FirstName = "Concurrent",
            LastName = "Administrator",
            CreatedBy = scope.ActorId
        };

        var raceToken = BT.Infrastructure.Security.SingleUseToken.Create();
        race.Issue(BT.Infrastructure.Security.SingleUseToken.Hash(raceToken), DateTimeOffset.UtcNow.AddHours(48));
        control.AdministratorInvitations.Add(race);
        await control.SaveChangesAsync();
        // A stale original revision models another process changing the invitation after it was read.
        control.Entry(race).Property(item => item.Revision).OriginalValue = Guid.CreateVersion7();
        var stale = await acceptance.AcceptPasswordAsync(new(race.Id, raceToken, "Concurrent-Password123!"), CancellationToken.None);
        Assert.False(stale.IsSuccess);
        var racingUser = await iam.Users.SingleAsync(account => account.Email == race.Email);
        Assert.True(racingUser.RequiresOnboarding);
        Assert.Empty(await users.GetRolesAsync(racingUser));
        control.ChangeTracker.Clear();

        // Exercise the real repository/UoW handlers after detaching all entities.
        // FirstOrDefaultAsync is intentionally no-tracking; write handlers must load tracked records.
        var tenantRepository = Substitute.For<Domain.Features.ControlPlane.Tenants.Contracts.Repositories.ITenantRepository>();
        tenantRepository.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(call => control.Tenants.SingleOrDefaultAsync(item => item.Id == tenant.Id, call.Arg<CancellationToken>()));

        await using var realControlUnit = new Persistence.Features.ControlPlane.ControlPlaneUnitOfWork(
            control,
            tenantRepository,
            Substitute.For<Domain.Features.ControlPlane.Tenants.Contracts.Repositories.IDeploymentStampRepository>(),
            Substitute.For<Domain.Features.ControlPlane.Auditing.Contracts.Repositories.IImpersonationRecordRepository>());

        var revoke = new Infrastructure.Features.ControlPlane.Tenants.CommandHandlers.RevokeAdministratorInvitationCommandHandler(TimeProvider.System, realControlUnit);
        Assert.True((await revoke.Handle(new(tenant.Id, external.Id), CancellationToken.None)).IsSuccess);
        control.ChangeTracker.Clear();
        var revoked = await control.AdministratorInvitations.AsNoTracking().SingleAsync(item => item.Id == external.Id);
        Assert.Equal(InvitationAcceptanceStatus.Revoked, revoked.AcceptanceStatus);
        Assert.False((await acceptance.CompleteAsync(externalUser.Id, CancellationToken.None)).IsSuccess);
        control.ChangeTracker.Clear();
        var invite = new Infrastructure.Features.ControlPlane.Tenants.CommandHandlers.InviteTenantAdministratorCommandHandler(TimeProvider.System, realControlUnit);
        Assert.True((await invite.Handle(new(tenant.Id, new(external.Email, external.FirstName, external.LastName), scope.ActorId), CancellationToken.None)).IsSuccess);
        control.ChangeTracker.Clear();
        Assert.Equal(InvitationAcceptanceStatus.Pending, (await control.AdministratorInvitations.AsNoTracking().SingleAsync(item => item.Id == external.Id)).AcceptanceStatus);
        var retry = new Infrastructure.Features.ControlPlane.Tenants.CommandHandlers.RetryAdministratorInvitationCommandHandler(TimeProvider.System, realControlUnit);
        Assert.True((await retry.Handle(new(tenant.Id, race.Id), CancellationToken.None)).IsSuccess);
        control.ChangeTracker.Clear();
        Assert.Null((await control.AdministratorInvitations.AsNoTracking().SingleAsync(item => item.Id == race.Id)).TokenHash);
    }

    private static DbContextOptions<TContext> Options<TContext>(DbFixture fixture, string history) where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();

        if (fixture is PostgreSqlDbFixture)
        {
            builder.UseNpgsql(fixture.GetConnectionString(), provider => provider.MigrationsHistoryTable("__EFMigrationsHistory_" + history))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
        }
        else
        {
            builder.UseSqlServer(fixture.GetConnectionString(), provider => provider.MigrationsHistoryTable("__EFMigrationsHistory_" + history))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
        }

        return builder.Options;
    }

    private static async Task CreatePreviousSchemaAsync(DbContext context, string previous)
    {
        // This test exercises an existing database upgrade. Historical IAM migrations include
        // seed updates before their schema baseline, so replaying them on an empty database
        // does not represent the pre-upgrade database we need to verify.
        var database = context.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();

        if (!await database.ExistsAsync())
        {
            await database.CreateAsync();
        }

        var assembly = context.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[previous], context.Database.ProviderName!);
        var model = context.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel, designTime: true);
        var operations = context.GetService<IMigrationsModelDiffer>().GetDifferences(null, model.GetRelationalModel())
            // Keep the IAM reference/menu catalog; other contexts use only explicit legacy fixtures.
            .Where(operation => context is IamDBContext || operation is not Microsoft.EntityFrameworkCore.Migrations.Operations.InsertDataOperation)
            .ToArray();

        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(operations, model))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }

        var history = context.GetService<IHistoryRepository>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());

        foreach (var name in context.Database.GetMigrations().TakeWhile(name => string.CompareOrdinal(name, previous) <= 0))
        {
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(name, "10.0.11")));
        }
    }

    private sealed class TestScope : ICurrentTenantProvider, ICurrentActorProvider
    {
        public Guid TenantId
        {
            get; set;
        }

        public string ActorId => "00000000-0000-0000-0000-000000000001";
    }
}
