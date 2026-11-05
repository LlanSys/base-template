using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Permissions.Commands;



internal sealed class DeletePermissionCommandHandler(TimeProvider timeProvider, IIamUnitOfWork unitOfWork, ILogger<DeletePermissionCommandHandler> logger)
    : IRequestHandler<DeletePermissionCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeletePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await unitOfWork.PermissionRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);

            if (permission is null)
            {
                return AppResponses.Failure<bool>($"Permission {command.Id} not found.");
            }

            if (BT.SharedKernel.Features.IAM.Permissions.ImplementedPermissions.IsProtected(permission.Key))
                return AppResponses.Failure<bool>("This permission protects shipped functionality. Retire it through a reviewed application release; change assignments or availability to restrict access.");

            if (await unitOfWork.PermissionRepository.HasReferencesAsync(permission.Key, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<bool>("Remove menu references and role/direct-user assignments before retiring this permission.");

            permission.MarkAsDeleted(command.UserId, timeProvider.GetUtcNow());
            await unitOfWork.PermissionRepository.UpdateAsync(permission, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Permission deleted.", true)
                : AppResponses.Failure<bool>("Permission delete failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(DeletePermissionCommandHandler), ex);
            throw;
        }
    }
}
