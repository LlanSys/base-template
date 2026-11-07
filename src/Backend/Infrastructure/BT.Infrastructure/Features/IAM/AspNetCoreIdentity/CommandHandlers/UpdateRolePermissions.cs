using BT.Application.Features.IAM.Permissions.Commands;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateRolePermissions(
    RoleManager<AppRole> roleManager,
    IamDBContext context,
    ILogger<UpdateRolePermissions> logger,
    IamAccessBoundary boundary)
    : IRequestHandler<UpdateRolePermissionsCommand, AppResponse<RolePermissionsResponse>>
{
    public Task<AppResponse<RolePermissionsResponse>> Handle(UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        return IdentityWriteScope.RunAsync(
            context,
            () => HandleCore(command, cancellationToken),
            cancellationToken);
    }

    private async Task<AppResponse<RolePermissionsResponse>> HandleCore(UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var role = await roleManager.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(role => role.Id == command.RoleId && !role.IsDeleted, cancellationToken).ConfigureAwait(false);

            if (role is null)
            {
                return AppResponses.Failure<RolePermissionsResponse>("Role not found.");
            }

            var requestedKeys = command.Request.PermissionKeys
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Select(static key => key.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!await boundary.CanEditRoleAsync(command.UserId, role, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<RolePermissionsResponse>(AppError.Forbidden("You cannot manage assignments for this role."));
            }

            var currentKeys = ((await roleManager.GetClaimsAsync(role).ConfigureAwait(false))
                .Where(static claim => claim.Type == "permission")
                .Select(static claim => claim.Value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (command.Request.OriginalPermissionKeys is { } originalKeys && !currentKeys.SetEquals(originalKeys))
            {
                return AppResponses.Failure<RolePermissionsResponse>("Assignments changed. Reopen the dialog before saving.");
            }

            var addedKeys = requestedKeys.Except(currentKeys, StringComparer.OrdinalIgnoreCase).ToArray();

            if (addedKeys.Length > 0
                && !await boundary.CanGrantPermissionsAsync(command.UserId, addedKeys, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<RolePermissionsResponse>(AppError.Forbidden("One or more new permissions are outside your delegated access."));
            }

            var validKeys = await context.Permissions
                .AsNoTracking()
                .Where(permission => addedKeys.Contains(permission.Key) && permission.IsActive)
                .Where(permission => permission.DepartmentId == null || permission.DepartmentId == role.DepartmentId)
                .Select(static permission => permission.Key)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var invalidKeys = addedKeys
                .Except(validKeys, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (invalidKeys.Count > 0)
            {
                return AppResponses.Failure<RolePermissionsResponse>($"Unknown, inactive, or out-of-scope permission(s): {string.Join(", ", invalidKeys)}.");
            }

            await boundary.SetRolePermissionsAsync(role, requestedKeys, cancellationToken).ConfigureAwait(false);

            return AppResponses.Success(
                "Role permissions updated.",
                new RolePermissionsResponse(role.Id, role.Name ?? string.Empty, requestedKeys));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleUpdateError(logger, command.RoleId, ex);
            throw;
        }
    }
}
