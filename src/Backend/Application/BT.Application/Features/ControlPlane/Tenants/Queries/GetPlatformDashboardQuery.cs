using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Queries;

public sealed record GetPlatformDashboardQuery(string ActorId) : IRequest<AppResponse<PlatformDashboardResponse>>, ICachableRequest
{
    public string CacheGroup => "platform-dashboard";
    public string Discriminator => ActorId;
    public string? CacheUserId => ActorId;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
