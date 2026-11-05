using BT.Application.Features.IAM.Users.Commands;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Logging;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using BT.Infrastructure.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RevokeEmployeeSystemAccess(
    TimeProvider timeProvider,
    UserManager<AppUser> userManager,
    IIamUnitOfWork iamUnitOfWork,
    ISessionService sessionService,
    ILogger<RevokeEmployeeSystemAccess> logger,
    IamAccessBoundary boundary)
    : IRequestHandler<RevokeEmployeeSystemAccessCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(RevokeEmployeeSystemAccessCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.Users
                .SingleOrDefaultAsync(item => item.EmployeeId == command.EmployeeId, cancellationToken)
                .ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.Failure<bool>("This employee is not linked to an IAM user account.");
            }

            if (user.Id == command.RevokedBy || !await boundary.CanAccessUserAsync(command.RevokedBy, user, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<bool>(AppError.Forbidden("You cannot revoke your own access or an account outside your administrative scope."));

            if (!user.IsActive)
            {
                return AppResponses.Success("Employee system access is already inactive.", true);
            }

            var reason = string.IsNullOrWhiteSpace(command.Request.Reason)
                ? "Revoked by administrator"
                : command.Request.Reason.Trim();

            user.RevokeAccess(timeProvider.GetUtcNow(), command.RevokedBy, reason);
            var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);

            if (!updateResult.Succeeded)
            {
                return AppResponses.Failure<bool>(updateResult.Errors.First().Description);
            }

            await sessionService.RevokeAllUserSessionsAsync(user.Id, null, cancellationToken).ConfigureAwait(false);
            await iamUnitOfWork.TokenRepository
                .RevokeAllUserTokensAsync(user.Id, "Employee system access revoked")
                .ConfigureAwait(false);

            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("Employee system access revoked. Active sessions and refresh tokens have been terminated.", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogDeactivateUserError(logger, command.EmployeeId.ToString(), ex);
            throw;
        }
    }
}
