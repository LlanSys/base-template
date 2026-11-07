using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateUserPermissions(
    UserManager<AppUser> userManager,
    IamDBContext context,
    ILogger<UpdateUserPermissions> logger,
    IamAccessBoundary boundary)
    : IRequestHandler<UpdateUserPermissionsCommand, AppResponse<UserPermissionsResponse>>
{
    public Task<AppResponse<UserPermissionsResponse>> Handle(UpdateUserPermissionsCommand command, CancellationToken cancellationToken)
    {
        return IdentityWriteScope.RunAsync(
            context,
            () => HandleCore(command, cancellationToken),
            cancellationToken);
    }

    private async Task<AppResponse<UserPermissionsResponse>> HandleCore(UpdateUserPermissionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.Failure<UserPermissionsResponse>("User not found.");
            }

            var requestedKeys = command.Request.PermissionKeys
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Select(static key => key.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!await boundary.CanAccessUserAsync(command.UpdatedBy, user, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<UserPermissionsResponse>(AppError.Forbidden("You cannot manage assignments for this user."));
            }

            var currentKeys = ((await userManager.GetClaimsAsync(user).ConfigureAwait(false))
                .Where(static claim => claim.Type == "permission")
                .Select(static claim => claim.Value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (command.Request.OriginalPermissionKeys is { } originalKeys && !currentKeys.SetEquals(originalKeys))
            {
                return AppResponses.Failure<UserPermissionsResponse>("Assignments changed. Reopen the dialog before saving.");
            }

            var addedKeys = requestedKeys.Except(currentKeys, StringComparer.OrdinalIgnoreCase).ToArray();

            if (addedKeys.Length > 0
                && !await boundary.CanGrantPermissionsAsync(command.UpdatedBy, addedKeys, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<UserPermissionsResponse>(AppError.Forbidden("One or more new permissions are outside your delegated access."));
            }

            var validKeys = await context.Permissions
                .AsNoTracking()
                .Where(permission => addedKeys.Contains(permission.Key) && permission.IsActive)
                .Select(static permission => permission.Key)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var invalidKeys = addedKeys.Except(validKeys, StringComparer.OrdinalIgnoreCase).ToList();

            if (invalidKeys.Count > 0)
            {
                return AppResponses.Failure<UserPermissionsResponse>($"Unknown or inactive permission(s): {string.Join(", ", invalidKeys)}.");
            }

            await boundary.SetUserPermissionsAsync(user, requestedKeys, command.UpdatedBy, cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("User permissions updated.", new UserPermissionsResponse(user.Id, user.UserName ?? user.Email ?? user.Id, requestedKeys));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
