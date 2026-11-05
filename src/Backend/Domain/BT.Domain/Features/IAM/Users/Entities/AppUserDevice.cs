using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;

namespace BT.Domain.Features.IAM.Users.Entities;

public class AppUserDevice : BaseEntity, ISoftDeletable
{
    [MaxLength(450)]
    public string AppUserId { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public bool IsTrusted { get; set; }
    public DateTimeOffset? TrustedUntil { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
        SetUpdatedInfo(now, deletedBy);
    }

    public void RevokeTrust(DateTimeOffset now, string revokedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedBy);

        IsTrusted = false;
        TrustedUntil = null;
        SetUpdatedInfo(now, revokedBy);
    }

    [JsonIgnore]
    public virtual AppUser AppUser { get; set; } = null!;
}
