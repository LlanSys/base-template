using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Domain.Features.Banking.Customers.Events;

public sealed record CustomerCreatedEvent(
    Guid CustomerId,
    string Number,
    string Name,
    string Email,
    CustomerType Type

) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
