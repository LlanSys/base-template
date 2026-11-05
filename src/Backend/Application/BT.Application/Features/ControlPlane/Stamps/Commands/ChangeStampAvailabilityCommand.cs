using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Application.Features.ControlPlane.Stamps.Commands;

public sealed record ChangeStampAvailabilityCommand(Guid Id, bool IsActive, bool Delete = false) : IRequest<AppResponse<bool>>, Contracts.Interfaces.Common.ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [BT.Application.Utilities.CacheKeys.GroupVersion("deployment-stamps")];
}
