using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;

using MediatR;
namespace BT.Application.Features.IAM.Users.Queries;

public sealed record GetRoleAssignedUsersQuery(string RoleId, string ActorId, string? Cursor = null) : IRequest<AppResponse<AssignedUsersPage>>, ICachableRequest
{
    public string CacheGroup => "iam-admin";
    public string Discriminator => RoleId + ":" + Cursor;
    public string? CacheUserId => ActorId;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
