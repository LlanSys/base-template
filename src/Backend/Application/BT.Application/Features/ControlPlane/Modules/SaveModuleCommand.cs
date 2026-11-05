using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

public sealed record SaveModuleCommand(Guid? Id, ModuleDefinitionRequest Request, string ActorId) : IRequest<AppResponse<ModuleDefinitionResponse>>, Contracts.Interfaces.Common.ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [BT.Application.Utilities.CacheKeys.GroupVersion("modules"), BT.Application.Utilities.CacheKeys.GroupVersion("menus")];
}
