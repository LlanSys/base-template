using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class RevokeAdministratorInvitationCommandHandler(TimeProvider timeProvider, IControlPlaneUnitOfWork unitOfWork) : IRequestHandler<RevokeAdministratorInvitationCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(RevokeAdministratorInvitationCommand request, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.AdministratorInvitations.FindByIdAsync(request.InvitationId, cancellationToken).ConfigureAwait(false);

        if (item is null || item.TenantId != request.TenantId || !item.Revoke(timeProvider.GetUtcNow())) return AppResponses.Failure<bool>("This invitation cannot be revoked.");
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success("Invitation revoked. It can no longer be accepted.", true);
    }
}
