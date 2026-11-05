using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

public sealed record GetModulesQuery : IRequest<AppResponse<IReadOnlyList<ModuleDefinitionResponse>>>, Contracts.Interfaces.Common.ICachableRequest
{
    public string CacheGroup => "modules";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
    public bool BypassCache => true;
}
