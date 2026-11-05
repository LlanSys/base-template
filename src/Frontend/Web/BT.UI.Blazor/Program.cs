using BT.UI.Blazor.Components;
using BT.UI.Blazor.Components.Security;
using Microsoft.AspNetCore.Components.Authorization;
using BT.UI.Blazor.Configuration;
using BT.UI.Blazor.Features.Banking.Customers.Contracts.Implementations;
using BT.UI.Blazor.Features.HR.Departments.Contracts.Implementations;
using BT.UI.Blazor.Features.HR.Employees.Contracts.Implementations;
using BT.UI.Blazor.Features.IAM.Users.Contracts.Implementations;
using BT.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using BT.UI.Blazor.Features.IAM.Users.Implementations;
using BT.UI.Blazor.Features.Shared.BackendApi.Contracts.Implementations;
using BT.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using BT.UI.Blazor.Features.Shared.Lookups.Contracts.Implementations;
using BT.UI.Blazor.Features.Shared.Messaging;
using BT.UI.Blazor.Features.ControlPlane.Tenants.Contracts;
using BT.UI.Blazor.Features.ControlPlane.Tenants.Implementations;
using BT.UI.Blazor.Features.ControlPlane.Auditing.Contracts;
using BT.UI.Blazor.Features.ControlPlane.Auditing.Implementations;
using BT.UI.Blazor.Features.ControlPlane.Stamps.Contracts;
using BT.UI.Blazor.Features.ControlPlane.Stamps.Implementations;
using BT.UI.Blazor.Features.Shared.Payments.Contracts.Implementations;
using BT.UI.Blazor.Features.Banking.Customers.Contracts.Interfaces;
using BT.UI.Blazor.Features.HR.Departments.Contracts.Interfaces;
using BT.UI.Blazor.Features.HR.Employees.Contracts.Interfaces;
using BT.UI.Blazor.Features.Shared.Lookups.Contracts.Interfaces;
using BT.UI.Blazor.Features.Shared.Payments.Contracts.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using System.Globalization;
using BT.SharedKernel.Features.ControlPlane;
using BT.UI.Blazor.Features.IAM.Users.State;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
ConfigurePlatformAssignedPort(builder.WebHost);

UserMessageSanitizer.IsDevelopment = builder.Environment.IsDevelopment();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment()
            && builder.Configuration.GetValue("DetailedErrors", false);
    });

ConfigureDataProtection(builder.Services, builder.Configuration);

builder.Services.AddMudServices();

builder.Services.AddAuthorizationCore(
    options =>
    {
        options.AddPolicy(
            "ControlPlane.Access",
            policy =>
                policy.RequireAssertion(context => context.User.HasClaim("platform_staff", "true")
                    && PlatformPermissions.OpensControlPanel(
                        context.User.FindAll("permission").Select(claim => claim.Value))));

        foreach (var permission in PlatformPermissions.Operations.Concat(["modules.manage", "modules.create", "menus.view", "roles.view", "users.view", "permissions.manage"]))
        {
            options.AddPolicy(permission, policy => policy.RequireClaim("platform_staff", "true").RequireClaim("permission", permission));
        }

        options.AddPolicy(
            "Platform.AccessManagement",
            policy => policy.RequireClaim("platform_staff", "true")
                .RequireAssertion(context => context.User.FindAll("permission").Any(claim => claim.Value is "roles.view" or "roles.manage_permissions" or "users.view")));

        options.AddPolicy(
            "Tenant.Administration",
            policy => policy.RequireAssertion(context =>
                !context.User.HasClaim("platform_staff", "true") && context.User.Claims.Any(claim => claim.Type == "permission"
                    && (claim.Value.StartsWith("users.", StringComparison.Ordinal)
                        || claim.Value.StartsWith("roles.", StringComparison.Ordinal)
                        || claim.Value.StartsWith("tenant-settings.", StringComparison.Ordinal)))));
    });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services
    .AddOptions<BackendApiSettings>()
    .Bind(builder.Configuration.GetSection(BackendApiSettings.SectionName))
    .ValidateDataAnnotations()
    .Validate(settings => Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _), "Backend API base URL must be absolute.")
    .ValidateOnStart();

builder.Services
    .AddOptions<SessionLifecycleSettings>()
    .Bind(builder.Configuration.GetSection(SessionLifecycleSettings.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        settings => settings.WarningBeforeTimeoutMinutes < settings.IdleTimeoutMinutes,
        "Session lifecycle warning must happen before the idle timeout.")
    .Validate(
        settings => settings.KeepAliveIntervalMinutes < settings.IdleTimeoutMinutes,
        "Session lifecycle keep-alive interval must be shorter than the idle timeout.")
    .ValidateOnStart();

// Protected browser storage is the durable browser source. The scoped server
// store only bridges periods where JS interop is unavailable within a circuit.
builder.Services.AddScoped<ITokenStorage, TokenStorage>();
builder.Services.AddScoped<IServerTokenStore, ServerTokenStore>();

builder.Services.AddScoped<IAuthSession, AuthSession>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PasswordRecoveryState>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IIamAdminService, IamAdminService>();
builder.Services.AddScoped<BT.UI.Blazor.Features.ControlPlane.Modules.ModuleService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IPaymentCheckoutService, PaymentCheckoutService>();
builder.Services.AddScoped<BT.UI.Blazor.Features.Shared.OrgSettings.Contracts.IOrgSettingsService, BT.UI.Blazor.Features.Shared.OrgSettings.Implementations.OrgSettingsService>();
builder.Services.AddSingleton<IAuthenticatorQrCodeService, AuthenticatorQrCodeService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDeploymentStampService, DeploymentStampService>();
var httpClientBuilder = builder.Services.AddHttpClient<IBackendApiClient, BackendApiClient>(
    (serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<BackendApiSettings>>().Value;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

if (builder.Environment.IsDevelopment())
{
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.Use(
    async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/iam/invitations", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/iam/sso-callback", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers.CacheControl = "no-store";
        }

        await next(context).ConfigureAwait(false);
    });

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    // Only an empty shell is served over HTTP. Native Blazor policies run after
    // protected browser storage is restored; all data remains behind API authorization.
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

app.Run();

static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<DataProtectionSettings>()
        .Bind(configuration.GetSection(DataProtectionSettings.SectionName))
        .Validate(
            settings => !string.IsNullOrWhiteSpace(settings.ApplicationName),
            "DataProtection:ApplicationName is required and must remain stable after protected data has been issued.")
        .ValidateOnStart();

    var settings = configuration
        .GetSection(DataProtectionSettings.SectionName)
        .Get<DataProtectionSettings>();

    var applicationName = string.IsNullOrWhiteSpace(settings?.ApplicationName)
        ? "BaseTemplate"
        : settings.ApplicationName.Trim();

    var dataProtectionBuilder = services.AddDataProtection()
        .SetApplicationName(applicationName);

    if (string.IsNullOrWhiteSpace(settings?.KeysPath))
    {
        return;
    }

    Directory.CreateDirectory(settings.KeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(settings.KeysPath));
}

static void ConfigurePlatformAssignedPort(ConfigureWebHostBuilder webHost)
{
    var port = Environment.GetEnvironmentVariable("PORT");

    if (string.IsNullOrWhiteSpace(port))
    {
        return;
    }

    if (!int.TryParse(port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort) ||
        parsedPort <= 0)
    {
        throw new InvalidOperationException("The PORT environment variable must be a positive integer.");
    }

    webHost.UseUrls($"http://+:{parsedPort}");
}
