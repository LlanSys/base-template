using System;
using BT.Domain.Shared.Contracts.Common;

namespace BT.Domain.Features.ControlPlane.Tenants.Events;

public record TenantStampChangedDomainEvent(
    Guid TenantId,
    Guid? OldStampId,
    Guid NewStampId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
