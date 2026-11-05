using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.IAM.Users.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
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
        => IdentityWriteScope.RunAsync(context,
            () => HandleCore(command, cancellationToken), cancellationToken);

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

            if (!await boundary.CanAccessUserAsync(command.UpdatedBy, user, cancellationToken).ConfigureAwait(false)
                || !await boundary.CanAssignRolesAsync(command.UpdatedBy, requestedRoles, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<UserRolesResponse>(AppError.Forbidden("Assignments must remain within your organization and delegated access."));

            if (!await boundary.SetUserRolesAsync(user, requestedRoles, command.UpdatedBy, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<UserRolesResponse>("The selected roles changed. Reload and try again.");

            return AppResponses.Success("User roles updated.", new UserRolesResponse(user.Id, user.UserName ?? user.Email ?? user.Id, requestedRoles));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
