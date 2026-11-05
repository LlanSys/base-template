using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Permissions.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Permissions.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Permissions.Commands;



internal sealed class UpdatePermissionCommandHandler(TimeProvider timeProvider, IIamUnitOfWork unitOfWork, ILogger<UpdatePermissionCommandHandler> logger)
    : IRequestHandler<UpdatePermissionCommand, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(UpdatePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var catalogError = await ValidateCatalogAsync(unitOfWork, command.Request.Context, command.Request.Resource, command.Request.Action, cancellationToken)
                .ConfigureAwait(false);

            if (catalogError is not null)
            {
                return AppResponses.Failure<PermissionResponse>(catalogError);
            }

            var permission = await unitOfWork.PermissionRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);

            if (permission is null)
            {
                return AppResponses.Failure<PermissionResponse>($"Permission {command.Id} not found.");
            }

            var draft = Permission.Create(
                command.Request.DepartmentId,
                command.Request.Context,
                command.Request.Resource,
                command.Request.Action,
                command.Request.Description,
                command.UserId);

            var duplicate = await unitOfWork.PermissionRepository
                // Permission keys are contracts used by compiled endpoints and existing grants.
                .AnyAsync(existing => existing.Id != command.Id && existing.Key == draft.Key, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<PermissionResponse>($"Permission {draft.Key} already exists.");
            }

            if (!string.Equals(permission.Key, draft.Key, StringComparison.Ordinal))
                return AppResponses.Failure<PermissionResponse>("Permission keys cannot be renamed. Register new functionality through a release.");

            if (!command.Request.IsActive)
            {
                if (BT.SharedKernel.Features.IAM.Permissions.ImplementedPermissions.IsProtected(permission.Key))
                    return AppResponses.Failure<PermissionResponse>("This permission protects shipped functionality. Retire it through a reviewed application release; change assignments or availability to restrict access.");

                if (await unitOfWork.PermissionRepository.HasReferencesAsync(permission.Key, cancellationToken).ConfigureAwait(false))
                    return AppResponses.Failure<PermissionResponse>("Remove menu references and role/direct-user assignments before retiring this permission.");
            }

            permission.Update(
                timeProvider.GetUtcNow(),
                command.Request.DepartmentId,
                command.Request.Context,
                command.Request.Resource,
                command.Request.Action,
                command.Request.Description,
                command.Request.IsActive,
                command.UserId);

            permission.AvailableTenantId = command.Request.AvailableTenantId;
            permission.IsTenantDelegatable = command.Request.IsTenantDelegatable;
            permission.RequiredModule = command.Request.RequiredModule ?? BT.SharedKernel.Features.ControlPlane.Modules.ModuleCatalog.ForPermission(permission.Key);
            await unitOfWork.PermissionRepository.UpdateAsync(permission, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Permission updated.", permission.ToPermissionResponse())
                : AppResponses.Failure<PermissionResponse>("Permission update failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(UpdatePermissionCommandHandler), ex);
            throw;
        }
    }

    private static async Task<string?> ValidateCatalogAsync(
        IIamUnitOfWork unitOfWork,
        string context,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var normalizedResource = resource.Trim().ToLowerInvariant().Replace(' ', '_');
        var normalizedAction = action.Trim().ToLowerInvariant().Replace(' ', '_');
        var normalizedContext = context.Trim();

        var contextExists = await unitOfWork.PermissionContextRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedContext, cancellationToken)
            .ConfigureAwait(false);

        if (!contextExists)
        {
            return $"Permission context '{context}' is not registered.";
        }

        var resourceExists = await unitOfWork.PermissionResourceRepository
            .AnyAsync(item => item.IsActive && item.ContextKey == normalizedContext && item.Key == normalizedResource, cancellationToken)
            .ConfigureAwait(false);

        if (!resourceExists)
        {
            return $"Permission resource '{resource}' is not registered for context '{context}'.";
        }

        var actionExists = await unitOfWork.PermissionActionRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedAction, cancellationToken)
            .ConfigureAwait(false);

        return actionExists ? null : $"Permission action '{action}' is not registered.";
    }
}
