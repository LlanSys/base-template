using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.IAM.Users.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Features.IAM.Authorization;
using BT.Persistence.Features.IAM.DataContext;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateUserRoles(
    UserManager<AppUser> userManager,
    ILogger<UpdateUserRoles> logger,
    IamDBContext context,
    IamAccessBoundary boundary)
    : IRequestHandler<UpdateUserRolesCommand, AppResponse<UserRolesResponse>>
{
    public Task<AppResponse<UserRolesResponse>> Handle(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        return IdentityWriteScope.RunAsync(
            context,
            () => HandleCore(command, cancellationToken),
            cancellationToken);
    }

    private async Task<AppResponse<UserRolesResponse>> HandleCore(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.Failure<UserRolesResponse>("User not found.");
            }

            var requestedRoles = command.Request.Roles
                .Where(static role => !string.IsNullOrWhiteSpace(role))
                .Select(static role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!await boundary.CanAccessUserAsync(command.UpdatedBy, user, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<UserRolesResponse>(AppError.Forbidden("You cannot manage assignments for this user."));
            }

            var currentNames = await context.UserRoles
                .Where(assignment => assignment.UserId == user.Id)
                .Join(context.Roles.IgnoreQueryFilters(), assignment => assignment.RoleId, role => role.Id, (_, role) => role.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var currentRoles = currentNames.OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (command.Request.OriginalRoles is { } originalRoles && !currentRoles.SetEquals(originalRoles))
            {
                return AppResponses.Failure<UserRolesResponse>("Assignments changed. Reopen the dialog before saving.");
            }

            var addedRoles = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();

            if (addedRoles.Length > 0
                && !await boundary.CanAssignRolesAsync(command.UpdatedBy, addedRoles, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<UserRolesResponse>(AppError.Forbidden("One or more new roles are outside your delegated access."));
            }

            if (!await boundary.SetUserRolesAsync(user, requestedRoles, command.UpdatedBy, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<UserRolesResponse>("The selected roles changed. Reload and try again.");
            }

            return AppResponses.Success("User roles updated.", new UserRolesResponse(user.Id, user.UserName ?? user.Email ?? user.Id, requestedRoles));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
