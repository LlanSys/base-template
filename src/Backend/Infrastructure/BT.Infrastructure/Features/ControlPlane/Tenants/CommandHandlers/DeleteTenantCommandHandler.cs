using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class DeleteTenantCommandHandler(TimeProvider timeProvider, IControlPlaneUnitOfWork unitOfWork, ICurrentActorProvider actor)
    : IRequestHandler<DeleteTenantCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await unitOfWork.Tenants.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (tenant is null) return AppResponses.NotFound<bool>("Tenant not found.");
        tenant.MarkAsDeleted(actor.ActorId, timeProvider.GetUtcNow());
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }
}
