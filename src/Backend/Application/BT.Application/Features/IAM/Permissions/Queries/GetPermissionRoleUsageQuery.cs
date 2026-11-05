using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;

using MediatR;
namespace BT.Application.Features.IAM.Permissions.Queries;

public sealed record GetPermissionRoleUsageQuery(string Key, string ActorId) : IRequest<AppResponse<IReadOnlyList<RolePermissionUsageResponse>>>, Contracts.Interfaces.Common.ICachableRequest
{
    public string CacheGroup => "permissions";
    public string Discriminator => Key;
    public string? CacheUserId => ActorId;
    public bool IsVersioned => true;
    public bool BypassCache => true;
}
