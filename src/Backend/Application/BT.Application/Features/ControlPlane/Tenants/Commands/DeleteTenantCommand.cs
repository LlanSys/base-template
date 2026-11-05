using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public sealed record DeleteTenantCommand(Guid Id) : IRequest<AppResponse<bool>>, Application.Contracts.Interfaces.Common.ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [BT.Application.Utilities.CacheKeys.GroupVersion("tenants"), BT.Application.Utilities.CacheKeys.GroupVersion("menus")];
}
