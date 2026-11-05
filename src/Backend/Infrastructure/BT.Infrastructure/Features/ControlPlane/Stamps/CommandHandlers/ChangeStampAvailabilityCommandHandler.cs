using BT.Application.Features.ControlPlane.Stamps.Commands;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Infrastructure.Features.ControlPlane.Stamps.CommandHandlers;

internal sealed class ChangeStampAvailabilityCommandHandler(TimeProvider timeProvider, IControlPlaneUnitOfWork unitOfWork, ICurrentActorProvider actor)
    : IRequestHandler<ChangeStampAvailabilityCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(ChangeStampAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var stamp = await unitOfWork.DeploymentStamps.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (stamp is null) return AppResponses.NotFound<bool>("Deployment stamp not found.");

        if ((!request.IsActive || request.Delete) && await unitOfWork.Tenants.AnyAsync(
            tenant => tenant.DeploymentStampId == stamp.Id, cancellationToken).ConfigureAwait(false))
            return AppResponses.Failure<bool>("Move or retire the tenants assigned to this stamp before disabling or deleting it.");

        if (request.Delete) stamp.MarkAsDeleted(actor.ActorId, timeProvider.GetUtcNow());
        else stamp.IsActive = request.IsActive;
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }
}
