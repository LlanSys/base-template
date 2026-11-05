using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Auditing.Dtos;

using MediatR;
namespace BT.Application.Features.ControlPlane.Auditing.Queries;

public sealed record GetAdministrationAuditQuery(Guid? TenantId = null) : IRequest<AppResponse<IReadOnlyList<AdministrationAuditResponse>>>, Contracts.Interfaces.Common.ICachableRequest
{
    public string CacheGroup => "administration-audit";
    public string Discriminator => TenantId?.ToString() ?? "recent";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
    public bool BypassCache => true;
}
