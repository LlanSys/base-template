using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.IAM.Users.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Domain.Features.IAM.Users.Events;

public sealed record RequestPasswordResetEvent(
    Guid UserId,
    string FirstName,
    string PhoneNumber,
    string ValidationCode,
    string Email,
    string ResetToken,
    string ResetUrl,
    MfaChannel PreferredChannel

) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; }
}
