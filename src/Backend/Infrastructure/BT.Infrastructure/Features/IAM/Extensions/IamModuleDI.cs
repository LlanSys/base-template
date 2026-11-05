using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Configuration;
using BT.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using BT.Infrastructure.Features.Shared.Notifications.Contracts.Implementations.Services;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;
using BT.Infrastructure.Extensions;
using BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;
using BT.Infrastructure.Features.IAM.Users.Seeding;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.IAM.Extensions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BT.Application.Features.IAM.Authorization;
using BT.Application.Features.ControlPlane.Modules;
using BT.Infrastructure.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.Extensions;

public static class IamModuleDI
{
    public static IServiceCollection AddIamModule(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        services.TryAddSingleton(TimeProvider.System);

        services.AddIamPersistence(configuration, environment);

        services.Configure<DevelopmentSeedSettings>(configuration.GetSection(DevelopmentSeedSettings.SectionName));

        services.AddIdentity<AppUser, AppRole>(DependencyInjection.ConfigureIdentityOptions)
            .AddEntityFrameworkStores<IamDBContext>()
            .AddDefaultTokenProviders();

        services.RemoveAll<IRoleValidator<AppRole>>();
        services.AddScoped<IRoleValidator<AppRole>, ScopedRoleValidator>();

        DependencyInjection.ConfigureAuthentication(services, configuration);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Login>());

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IClaimsService, ClaimsService>();
        services.AddScoped<IEffectiveAccessService, EffectiveAccessService>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ISmsComposer, SmsComposer>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<DevelopmentIdentitySeeder>();
        services.AddScoped<PlatformCatalogInitializer>();
        services.AddScoped<IamAccessBoundary>();
        services.AddScoped<IModuleReadinessService, ModuleReadinessService>();

        return services;
    }
}
