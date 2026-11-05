using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Permissions.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Permissions.Queries;


public sealed record GetPermissionByIdQuery(Guid Id, string UserId) : IRequest<AppResponse<PermissionResponse>>, ICachableRequest
{
    public string CacheGroup => "permissions";

    public string Discriminator => CacheKeys.Entity("permissions", Id.ToString());

    public string? CacheUserId => UserId;

    public bool IsVersioned => true;
    public bool BypassCache => true;
}

