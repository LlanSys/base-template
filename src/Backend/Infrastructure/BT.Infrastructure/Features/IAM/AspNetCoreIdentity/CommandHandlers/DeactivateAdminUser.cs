using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Logging;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using BT.Infrastructure.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class DeactivateAdminUser(TimeProvider timeProvider, UserManager<AppUser> userManager, ILogger<DeactivateAdminUser> logger,
    IamAccessBoundary boundary)
    : IRequestHandler<DeactivateAdminUserCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeactivateAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.Failure<bool>("User not found.");
            }

            if (user.Id == command.DeactivatedBy || !await boundary.CanAccessUserAsync(command.DeactivatedBy, user, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<bool>(AppError.Forbidden("You cannot deactivate your own account or an account outside your administrative scope."));

            if (!user.IsActive)
            {
                return AppResponses.Success("User is already inactive.", true);
            }

            var reason = string.IsNullOrWhiteSpace(command.Request.Reason)
                ? "Deactivated by administrator"
                : command.Request.Reason.Trim();

            user.RevokeAccess(timeProvider.GetUtcNow(), command.DeactivatedBy, reason);
            var result = await userManager.UpdateAsync(user).ConfigureAwait(false);

            return result.Succeeded
                ? AppResponses.Success("User deactivated.", true)
                : AppResponses.Failure<bool>(string.Join(", ", result.Errors.Select(static error => error.Description)));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogDeactivateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
