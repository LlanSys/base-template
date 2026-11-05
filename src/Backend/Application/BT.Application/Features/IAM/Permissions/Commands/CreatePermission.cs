using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Permissions.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Permissions.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;
using BT.SharedKernel.Features.IAM.Permissions;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Permissions.Commands;

internal sealed class CreatePermissionCommandHandler(IIamUnitOfWork unitOfWork, ILogger<CreatePermissionCommandHandler> logger)
    : IRequestHandler<CreatePermissionCommand, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(CreatePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var catalogError = await ValidateCatalogAsync(unitOfWork, command.Request.Context, command.Request.Resource, command.Request.Action, cancellationToken)
                .ConfigureAwait(false);

            if (catalogError is not null)
            {
                return AppResponses.Failure<PermissionResponse>(catalogError);
            }

            var permission = Permission.Create(
                command.Request.DepartmentId,
                command.Request.Context,
                command.Request.Resource,
                command.Request.Action,
                command.Request.Description,
                command.UserId);

            if (ImplementedPermissions.IsRetired(permission.Key))
            {
                return AppResponses.Failure<PermissionResponse>("This permission has been retired. Select a specific platform operation.");
            }

            var duplicate = await unitOfWork.PermissionRepository
                // Definition availability is enforced by the backend's effective access evaluation.
                .AnyAsync(existing => existing.Key == permission.Key, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<PermissionResponse>($"Permission {permission.Key} already exists.");
            }

            permission.AvailableTenantId = command.Request.AvailableTenantId;
            permission.IsTenantDelegatable = command.Request.IsTenantDelegatable;
            permission.RequiredModule = command.Request.RequiredModule ?? ModuleCatalog.ForPermission(permission.Key);
            await unitOfWork.PermissionRepository.CreateAsync(permission, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Permission created.", permission.ToPermissionResponse())
                : AppResponses.Failure<PermissionResponse>("Permission create failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(CreatePermissionCommandHandler), ex);
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
        var contextExists = await unitOfWork.PermissionContextRepository
            .AnyAsync(item => item.IsActive && item.Key == context.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (!contextExists)
        {
            return $"Permission context '{context}' is not registered.";
        }

        var resourceExists = await unitOfWork.PermissionResourceRepository
            .AnyAsync(item => item.IsActive && item.ContextKey == context.Trim() && item.Key == resource.Trim().ToLowerInvariant().Replace(' ', '_'), cancellationToken)
            .ConfigureAwait(false);

        if (!resourceExists)
        {
            return $"Permission resource '{resource}' is not registered for context '{context}'.";
        }

        var actionExists = await unitOfWork.PermissionActionRepository
            .AnyAsync(item => item.IsActive && item.Key == action.Trim().ToLowerInvariant().Replace(' ', '_'), cancellationToken)
            .ConfigureAwait(false);

        return actionExists ? null : $"Permission action '{action}' is not registered.";
    }
}
