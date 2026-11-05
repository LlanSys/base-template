using BT.SharedKernel.Dtos.Common;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

public sealed record DeleteModuleCommand(Guid Id, string ActorId) : IRequest<AppResponse<bool>>, Contracts.Interfaces.Common.ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [BT.Application.Utilities.CacheKeys.GroupVersion("modules"), BT.Application.Utilities.CacheKeys.GroupVersion("menus")];
}
