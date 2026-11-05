using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.ValueObjects;
using MediatR;

namespace BT.Domain.Features.Shared.Payments.Events;

public sealed record PaymentCompletedEvent(
    Guid PaymentRecordId,
    string CustomerReference,
    string Provider,
    Money Amount) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
