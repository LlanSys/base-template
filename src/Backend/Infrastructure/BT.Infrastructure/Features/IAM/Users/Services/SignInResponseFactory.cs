using System.Security.Claims;
using BT.Application.Features.IAM.Users.Mappings;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Configuration;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Extensions;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.SharedKernel.Features.ControlPlane.Modules;
using BT.SharedKernel.Features.Shared.Common.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.Users.Services;

internal sealed class SignInResponseFactory(UserManager<AppUser> users, IJwtService jwt, IClaimsService claims,
    ISessionService sessions, IIamUnitOfWork unitOfWork, IHttpContextAccessor http, IOptions<JwtSettings> settings,
    IOptions<MfaSettings> mfaSettings, ControlPlaneDBContext control, TimeProvider timeProvider)
{
    public async Task<AppResponse<LoginResponse>> CreateAsync(AppUser user, CancellationToken cancellationToken)
    {
        if (!user.IsActive || user.IsDeleted || await users.IsLockedOutAsync(user).ConfigureAwait(false))
            return AppResponses.Failure<LoginResponse>("This account is unavailable. Contact your administrator.");

        if (!user.IsPlatformStaff && !await control.Tenants.AnyAsync(tenant => tenant.Id == user.TenantId && !tenant.IsDeleted
            && tenant.Status == TenantStatus.Active, cancellationToken).ConfigureAwait(false))
            return AppResponses.Failure<LoginResponse>("Sign-in is unavailable for this organization.");
        var roles = await users.GetRolesAsync(user).ConfigureAwait(false);
        var info = new AppUserResponse(user.Id, user.UserName ?? string.Empty, user.FirstName, user.LastName,
            $"{user.FirstName} {user.LastName}", user.PhoneNumber, user.NationalId, user.Email ?? string.Empty,
            user.Gender.ToDisplayString(), null, user.IsActive, user.TwoFactorEnabled, user.RequirePasswordChange,
            user.CreatedAt, user.LastLoginAt, [.. roles], user.TenantId, user.EmployeeId, user.CustomerId);

        if (user.TwoFactorEnabled)
        {
            var temporaryClaims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new("temp_auth", "true"), new("tenant_id", user.TenantId.ToString()) };
            var token = await jwt.CreateTempTokenAsync(temporaryClaims, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
            return AppResponses.Success("Verify your authenticator to continue.", new LoginResponse(user.Id, user.FirstName,
                user.LastName, user.Email ?? string.Empty, true, false, false, token, string.Empty, null,
                timeProvider.GetUtcNow().AddMinutes(10), info, temporaryClaims.ToClaimResponses(), user.RequiresOnboarding));
        }
        var session = await sessions.CreateSessionAsync(user.Id, Guid.CreateVersion7(),
            http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            http.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown", "Onboarding/SSO", cancellationToken).ConfigureAwait(false);

        if (!session.IsSuccess) return AppResponses.Failure<LoginResponse>("Could not establish a session. Please sign in again.");
        var userClaims = await claims.GetUserClaimsAsync(user, session.Data, cancellationToken).ConfigureAwait(false);
        var access = await jwt.CreateTokenAsync(userClaims).ConfigureAwait(false);
        var refresh = jwt.CreateRefreshToken();
        await unitOfWork.TokenRepository.AddRefreshTokenAsync(RefreshToken.Create(timeProvider.GetUtcNow(), user.Id, refresh,
            timeProvider.GetUtcNow().AddHours(settings.Value.RefreshTokenExpiryHours), user.Id,
            http.HttpContext?.Connection.RemoteIpAddress?.ToString())).ConfigureAwait(false);

        user.RecordSuccessfulLogin(timeProvider.GetUtcNow());
        var updated = await users.UpdateAsync(user).ConfigureAwait(false);

        if (!updated.Succeeded) return AppResponses.Failure<LoginResponse>("Could not establish a session. Please sign in again.");
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        var requiresMfa = user.RequiresOnboarding || (mfaSettings.Value.Enabled && mfaSettings.Value.EnforceEnrollment
            && (roles.Any(role => mfaSettings.Value.RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                || userClaims.Any(claim => claim.Type == "permission" && ModuleCatalog.IsAdministrativePermission(claim.Value))));

        return AppResponses.Success("Continue account setup.", new LoginResponse(user.Id, user.FirstName, user.LastName,
            user.Email ?? string.Empty, false, false, true, access, refresh, session.Data.ToString(),
            timeProvider.GetUtcNow().AddMinutes(settings.Value.AccessTokenExpiryMinutes), info, userClaims.ToClaimResponses(), requiresMfa));
    }
}
