using System;
using BT.Domain.Shared.Contracts.Common;

namespace BT.Domain.Features.ControlPlane.Tenants.Events;

public record TenantModuleRevokedDomainEvent(
    Guid TenantId,
    string ModuleKey) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
