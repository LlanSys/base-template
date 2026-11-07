using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

public sealed record SaveModuleCommand(Guid? Id, ModuleDefinitionRequest Request, string ActorId) : IRequest<AppResponse<ModuleDefinitionResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("modules"), CacheKeys.GroupVersion("menus"), CacheKeys.GroupVersion("iam-reference-data")];
}
