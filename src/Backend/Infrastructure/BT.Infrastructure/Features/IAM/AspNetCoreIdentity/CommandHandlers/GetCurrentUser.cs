using BT.SharedKernel.Extensions;
using BT.Application.Features.IAM.Users.Commands;
using BT.Application.Features.Banking.Customers.Mappings;
using BT.Application.Features.HR.Employees.Mappings;
using BT.Application.Features.IAM.Users.Mappings;
using BT.Application.Features.Shared.EmailTemplates.Mappings;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Configuration;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

using BT.Domain.Shared.Contracts.Settings;
using BT.Application.Features.IAM.Authorization;
using BT.SharedKernel.Features.ControlPlane.Modules;
using BT.SharedKernel.Features.Shared.OrgSettings;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class GetCurrentUser(
    IHttpContextAccessor httpContextAccessor,
    UserManager<AppUser> userManager,
    IOptions<MfaSettings> mfaSettings,
    ILogger<GetCurrentUser> logger,
    IOrgSettingsProvider settings,
    IEffectiveAccessService accessService) : IRequestHandler<GetCurrentUserCommand, AppResponse<CurrentUserResponse>>
{
    private readonly MfaSettings _mfaSettings = mfaSettings.Value;

    public async Task<AppResponse<CurrentUserResponse>> Handle(GetCurrentUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return AppResponses.Failure<CurrentUserResponse>("User not found.");

            var appUser = await userManager.FindByIdAsync(userId).ConfigureAwait(false);

            if (appUser == null)
                return AppResponses.Failure<CurrentUserResponse>("User not found.");

            var roles = await userManager.GetRolesAsync(appUser).ConfigureAwait(false);
            var rolesList = roles.ToList();

            var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(appUser).ConfigureAwait(false);
            var emailConfirmed = await userManager.IsEmailConfirmedAsync(appUser).ConfigureAwait(false);
            await userManager.IsPhoneNumberConfirmedAsync(appUser).ConfigureAwait(false);

            var lastLoginClaim = httpContextAccessor.HttpContext?.User?.FindFirst("LastLogin");

            DateTimeOffset? lastLoginAt = null;

            if (lastLoginClaim?.Value != null && DateTimeOffset.TryParse(lastLoginClaim.Value, out var lastLogin))
            {
                lastLoginAt = lastLogin;
            }

            bool? isAuthenticated = httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            var permissions = httpContextAccessor.HttpContext?.User.Claims
                .Where(static claim => string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase))
                .Select(static claim => claim.Value)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            var sessionId = httpContextAccessor.HttpContext?.User.FindFirstValue("session_id");
            var mfaEnrollmentRequired = appUser.RequiresOnboarding || IsMfaEnrollmentRequired(twoFactorEnabled, rolesList)
                || (_mfaSettings.Enabled && _mfaSettings.EnforceEnrollment && !twoFactorEnabled
                    && permissions.Any(ModuleCatalog.IsAdministrativePermission));

            var access = await accessService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
            var preferences = appUser.IsPlatformStaff ? OrganizationSettingCatalog.Defaults
                : new OrganizationPreferences(
                    await settings.GetSettingAsync("Organization.Name", cancellationToken).ConfigureAwait(false) ?? "BaseTemplate",
                    await settings.GetSettingAsync("Organization.TimeZone", cancellationToken).ConfigureAwait(false) ?? "UTC",
                    await settings.GetSettingAsync("Organization.Culture", cancellationToken).ConfigureAwait(false) ?? "en-KE",
                    await settings.GetSettingAsync("Organization.DateFormat", cancellationToken).ConfigureAwait(false) ?? "dd/MM/yyyy");

            return AppResponses.Success("CurrentUser", new CurrentUserResponse(
                userId,
                appUser.EmployeeId ?? Guid.Empty,
                appUser.CustomerId ?? Guid.Empty,
                appUser.NationalId ?? string.Empty,
                appUser.UserName ?? string.Empty,
                appUser.Email ?? string.Empty,
                appUser.FirstName ?? string.Empty,
                appUser.LastName ?? string.Empty,
                appUser.PhoneNumber ?? string.Empty,
                emailConfirmed,
                twoFactorEnabled,
                appUser.Gender.ToDisplayString(),
                isAuthenticated,
                lastLoginAt,
                rolesList,
                permissions,
                sessionId,
                mfaEnrollmentRequired,
                ProfilePictureUrlMapping.ToCurrentUserRoute(appUser.ProfilePictureUrl),
                appUser.IsPlatformStaff, preferences, access.Modules.ToArray()));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogGetCurrentUserError(logger, ex);
            throw;
        }
    }

    private bool IsMfaEnrollmentRequired(bool twoFactorEnabled, IEnumerable<string> roles)
    {
        if (!_mfaSettings.Enabled || !_mfaSettings.EnforceEnrollment || twoFactorEnabled)
        {
            return false;
        }

        return roles.Any(role => _mfaSettings.RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
