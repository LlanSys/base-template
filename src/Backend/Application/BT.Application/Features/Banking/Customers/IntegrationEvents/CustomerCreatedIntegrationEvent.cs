using BT.Application.Contracts.Interfaces.Common;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Application.Features.Banking.Customers.IntegrationEvents;

public record CustomerCreatedIntegrationEvent(
    Guid CustomerId,
    string Number,
    string Name,
    string Email,
    string Type) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
