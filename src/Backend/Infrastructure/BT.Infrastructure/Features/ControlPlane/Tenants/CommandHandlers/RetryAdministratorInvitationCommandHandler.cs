using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class RetryAdministratorInvitationCommandHandler(TimeProvider timeProvider, IControlPlaneUnitOfWork unitOfWork) : IRequestHandler<RetryAdministratorInvitationCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(RetryAdministratorInvitationCommand request, CancellationToken cancellationToken)
    {
        var tenant = await unitOfWork.Tenants.FindByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null) return AppResponses.NotFound<bool>("Tenant not found.");
        var invitation = await unitOfWork.AdministratorInvitations.FindByIdAsync(request.InvitationId, cancellationToken).ConfigureAwait(false);

        if (invitation is null || invitation.TenantId != request.TenantId) return AppResponses.NotFound<bool>("Invitation not found.");

        if (!invitation.Retry(timeProvider.GetUtcNow())) return AppResponses.Failure<bool>("The invitation is being processed. Try again shortly.");
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success("Invitation queued for delivery after activation.", true);
    }
}
