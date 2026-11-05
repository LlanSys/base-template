using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.ValueObjects;

namespace BT.Domain.Features.Shared.Payments.Events;

public sealed record PaymentCancelledEvent(
    Guid PaymentRecordId,
    string CustomerReference,
    string Provider,
    Money Amount,
    string Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
