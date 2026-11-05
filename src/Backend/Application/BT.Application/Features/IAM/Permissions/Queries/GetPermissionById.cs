using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Permissions.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Permissions.Queries;



internal sealed class GetPermissionByIdQueryHandler(IIamUnitOfWork unitOfWork, ILogger<GetPermissionByIdQueryHandler> logger,
    Authorization.IEffectiveAccessService accessService)
    : IRequestHandler<GetPermissionByIdQuery, AppResponse<PermissionResponse>>
{
    public async Task<AppResponse<PermissionResponse>> Handle(GetPermissionByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await unitOfWork.PermissionRepository.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
            var access = await accessService.GetAsync(query.UserId, cancellationToken).ConfigureAwait(false);
            return permission is null || (!access.IsPlatformStaff && !access.Permissions.Contains(permission.Key))
                ? AppResponses.NotFound<PermissionResponse>("Permission not found.")
                : AppResponses.Success(permission.ToPermissionResponse());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(GetPermissionByIdQueryHandler), ex);
            throw;
        }
    }
}
