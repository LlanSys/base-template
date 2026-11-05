using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;

namespace BT.Domain.Features.HR.Employees.Events;

public sealed record EmployeeCreatedEvent(
    Guid EmployeeId,
    string Number,
    string Email,
    string Name

) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
