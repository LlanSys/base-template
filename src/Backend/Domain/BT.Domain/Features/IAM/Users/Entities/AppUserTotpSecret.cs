using System;
using System.Collections.Generic;
using System.Text;

using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;

namespace BT.Domain.Features.IAM.Users.Entities;

public class AppUserTotpSecret : BaseEntity, ISoftDeletable
{
    public string AppUserId { get; private set; } = string.Empty;
    public string EncryptedSecret { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private AppUserTotpSecret() { }

    public static AppUserTotpSecret Create(
        DateTimeOffset now,
        string appUserId,
        string encryptedSecret,
        string createdBy,
        DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUserTotpSecret
        {
            Id = Guid.CreateVersion7(),
            AppUserId = appUserId,
            EncryptedSecret = encryptedSecret,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    public void MarkAsUsed(DateTimeOffset now)
    {
        LastUsedAt = now;
        SetUpdatedInfo(now, AppUserId);
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        SetUpdatedInfo(now, AppUserId);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
        IsActive = false;
        SetUpdatedInfo(now, deletedBy);
    }

    public virtual AppUser AppUser { get; set; } = null!;
}
