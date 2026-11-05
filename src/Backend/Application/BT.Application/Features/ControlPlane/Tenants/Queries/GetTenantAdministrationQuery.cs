using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Queries;

public sealed record GetTenantAdministrationQuery(Guid TenantId, string ActorId) : IRequest<AppResponse<TenantAdministrationResponse>>, ICachableRequest
{
    public string CacheGroup => "tenant-administration";
    public string Discriminator => TenantId.ToString();
    public string? CacheUserId => ActorId;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
