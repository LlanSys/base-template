using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;
using BT.Domain.Features.IAM.Users.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using BT.Infrastructure.Logging;
using BT.Application.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class ClaimsService(
    UserManager<AppUser> userManager,
    IEffectiveAccessService accessService, ILogger<ClaimsService> logger, TimeProvider timeProvider) : IClaimsService
{
    private readonly UserManager<AppUser> _userManager = userManager;

    private readonly ILogger<ClaimsService> _logger = logger;

    public async Task<bool> AddUserClaimAsync(AppUser user, Claim claim)
    {
        try
        {
            var result = await _userManager.AddClaimAsync(user, claim).ConfigureAwait(false);

            if (result.Succeeded)
            {
                ServiceLogDefinitions.LogClaimAdded(_logger, claim.Type, claim.Value, user.Id);

                return true;
            }

            ServiceLogDefinitions.LogFailedToAddClaim(_logger, claim.Type, claim.Value, user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));

            return false;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorAddingClaim(_logger, user.Id, ex);
            throw;
        }
    }

    public async Task<List<Claim>> GetUserClaimsAsync(AppUser user, Guid? sessionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var claims = new List<Claim>
            {
                // Standard Identity
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
                new(JwtRegisteredClaimNames.Iat, timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),

                new(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}"),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new("mfa_enrolled", user.TwoFactorEnabled ? "true" : "false"),
            };

            claims.Add(user.IsPlatformStaff ? new Claim("platform_staff", "true") : new Claim("tenant_id", user.TenantId.ToString()));

            if (sessionId.HasValue && sessionId.Value != Guid.Empty)
                claims.Add(new("session_id", sessionId.Value.ToString()));

            if (user.EmployeeId.HasValue)
                claims.Add(new("employee_id", user.EmployeeId.Value.ToString()));

            if (user.CustomerId.HasValue)
                claims.Add(new("customer_id", user.CustomerId.Value.ToString()));

            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            var access = await accessService.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
            claims.AddRange(access.Permissions.Select(permission => new Claim("permission", permission)));

            return [.. claims
                .GroupBy(static claim => $"{claim.Type}:{claim.Value}", StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())];
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogGetUserClaimsError(_logger, user.Id, ex);
            throw;
        }

    }

    public Task<List<Claim>> GetUserClaimsCombinedAsync(AppUser user, CancellationToken cancellationToken = default)
        => GetUserClaimsAsync(user, cancellationToken: cancellationToken);

    public async Task<bool> RemoveUserClaimAsync(AppUser user, Claim claim)
    {
        try
        {
            var result = await _userManager.RemoveClaimAsync(user, claim).ConfigureAwait(false);

            if (result.Succeeded)
            {
                ServiceLogDefinitions.LogClaimRemoved(_logger, claim.Type, claim.Value, user.Id);

                return true;
            }

            ServiceLogDefinitions.LogFailedToRemoveClaim(_logger, claim.Type, claim.Value, user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));

            return false;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorRemovingClaim(_logger, user.Id, ex);
            throw;
        }
    }

    public async Task<bool> UpdateUserClaimAsync(AppUser user, Claim existingClaim, Claim newClaim)
    {
        try
        {
            // Remove old claim and add new one
            var removeResult = await _userManager.RemoveClaimAsync(user, existingClaim).ConfigureAwait(false);

            if (!removeResult.Succeeded)
            {
                ServiceLogDefinitions.LogFailedToRemoveExistingClaim(_logger, user.Id);

                return false;
            }

            var addResult = await _userManager.AddClaimAsync(user, newClaim).ConfigureAwait(false);

            if (!addResult.Succeeded)
            {
                // Try to rollback by adding the old claim back
                await _userManager.AddClaimAsync(user, existingClaim).ConfigureAwait(false);

                ServiceLogDefinitions.LogFailedToAddNewClaimRolledBack(_logger, user.Id);

                return false;
            }

            var oldClaim = $"{existingClaim.Type}:{existingClaim.Value}";
            var replacementClaim = $"{newClaim.Type}:{newClaim.Value}";
            ServiceLogDefinitions.LogUpdatedClaim(_logger, user.Id, oldClaim, replacementClaim);

            return true;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorUpdatingClaim(_logger, user.Id, ex);
            throw;
        }
    }


}


