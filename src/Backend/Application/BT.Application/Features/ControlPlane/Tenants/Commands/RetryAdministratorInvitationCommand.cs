using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public sealed record RetryAdministratorInvitationCommand(Guid TenantId, Guid InvitationId) : IRequest<AppResponse<bool>>, Application.Contracts.Interfaces.Common.ICacheInvalidatorRequest
{
    public IEnumerable<string> CacheGroups => ["tenant-administration"];
    public string? CacheUserId => null;
}
