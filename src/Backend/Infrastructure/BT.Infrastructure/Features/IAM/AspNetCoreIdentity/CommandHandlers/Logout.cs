using BT.Application.Features.IAM.Users.Commands;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Features.IAM.Users.Events;
using BT.Infrastructure.Logging;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class Logout(
    TimeProvider timeProvider,
    SignInManager<AppUser> signInManager,
    IHttpContextAccessor httpContextAccessor,
    ISessionService sessionService,
    IIamUnitOfWork iamUnitOfWork,
    IPublisher publisher,
    ILogger<Logout> logger) : IRequestHandler<LogoutCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = httpContextAccessor.HttpContext?.Request.Headers["X-Session-Id"].FirstOrDefault()
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("session_id");

            ServiceLogDefinitions.LogUserSignedOut(logger, userId ?? string.Empty);

            await signInManager.SignOutAsync().ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    await sessionService.RevokeSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
                }

                await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
                {
                    await iamUnitOfWork.TokenRepository
                        .RevokeAllUserTokensAsync(userId, "User signed out", httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString())
                        .ConfigureAwait(false);

                    await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);

                await publisher.Publish(new UserLogoutEvent(userId, sessionId) { OccurredAt = timeProvider.GetUtcNow() }, cancellationToken).ConfigureAwait(false);
            }

            return AppResponses.Success("Signed out successfully", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUnexpectedTokenValidationError(logger, ex);
            throw;
        }
    }
}
