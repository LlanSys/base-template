using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.ValueObjects;
using MediatR;

namespace BT.Domain.Features.Shared.Payments.Events;

public sealed record PaymentFailedEvent(
    Guid PaymentRecordId,
    string CustomerReference,
    string Provider,
    Money Amount,
    string FailureReason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
