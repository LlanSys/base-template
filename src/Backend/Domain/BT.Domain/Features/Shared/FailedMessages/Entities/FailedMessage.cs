using BT.Domain.Features.Shared.FailedMessages.Enums;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Domain.Features.Shared.FailedMessages.Entities;

public class FailedMessage : BaseEntity, ISoftDeletable
{
    public string MessageId { get; private set; } = string.Empty;
    public string MessageType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; } // Generic - could be CustomerId, OrderId, etc.
    public string Payload { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public string? ErrorStackTrace { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset FailedAt { get; private set; }
    public FailedMessageStatus Status { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private FailedMessage() { }

    public static FailedMessage RecordPermanentFailure(
        DateTimeOffset now,
        string messageId,
        string messageType,
        string payload,
        string errorMessage,
        int retryCount,
        string createdBy,
        string? entityId = null,
        string? errorStackTrace = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new FailedMessage
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            MessageType = messageType,
            EntityId = entityId,
            Payload = payload,
            ErrorMessage = errorMessage,
            ErrorStackTrace = errorStackTrace,
            RetryCount = retryCount,
            FailedAt = now,
            Status = FailedMessageStatus.Permanent,
            CreatedBy = createdBy
        };
    }

    public void MarkResolved(DateTimeOffset now, string resolutionNotes, string resolvedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionNotes);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedBy);

        IsResolved = true;
        ResolvedAt = now;
        ResolutionNotes = resolutionNotes;
        SetUpdatedInfo(now, resolvedBy);
    }

    public void MarkForManualRetry(DateTimeOffset now, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Status = FailedMessageStatus.ManualRetry;
        SetUpdatedInfo(now, updatedBy);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
        SetUpdatedInfo(now, deletedBy);
    }
}
