using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Domain.Features.IAM.Users.Events;


public sealed record AppUserAccessRevokedEvent(
    string Id,
    Guid? EmployeeId,
    Guid? CustomerId,
    string RevokedBy,
    string RevokeReason

) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
