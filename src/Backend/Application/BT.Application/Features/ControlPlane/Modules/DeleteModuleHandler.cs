using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Dtos.Common;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

internal sealed class DeleteModuleHandler(TimeProvider timeProvider, IControlPlaneUnitOfWork unitOfWork) : IRequestHandler<DeleteModuleCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteModuleCommand command, CancellationToken cancellationToken)
    {
        var module = await unitOfWork.Modules.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);

        if (module is null) return AppResponses.Failure<bool>("Module not found.");
        module.IsActive = false;
        module.IsDeleted = true;
        module.DeletedAt = timeProvider.GetUtcNow();
        module.DeletedBy = command.ActorId;
        module.Revision = Guid.CreateVersion7();
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }
}
