using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.SharedKernel.Dtos.Common;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

public sealed record DeleteModuleCommand(Guid Id, string ActorId) : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("modules"), CacheKeys.GroupVersion("menus"), CacheKeys.GroupVersion("iam-reference-data")];
}
