using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public sealed record InviteTenantAdministratorCommand(Guid TenantId, InviteTenantAdministratorRequest Request, string ActorId) : IRequest<AppResponse<bool>>, Application.Contracts.Interfaces.Common.ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [BT.Application.Utilities.CacheKeys.GroupVersion("iam-admin")];
}
