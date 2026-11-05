using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

using BT.Domain.Shared.Entities;

namespace BT.Domain.Features.IAM.Users.Entities;

public class AppUserProfile : BaseEntity, ISoftDeletable
{
    public string? AppUserId { get; private set; }
    public string? TelephoneNo { get; private set; }
    public string? MobileNo { get; private set; }
    public string? Email { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private AppUserProfile() { }

    public static AppUserProfile Create(DateTimeOffset now, string appUserId, string? telephoneNo, string? mobileNo, string? email, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new AppUserProfile
        {
            Id = Guid.CreateVersion7(),
            AppUserId = appUserId,
            TelephoneNo = telephoneNo?.Trim(),
            MobileNo = mobileNo?.Trim(),
            Email = email?.Trim(),
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    public void UpdateContact(DateTimeOffset now, string? telephoneNo, string? mobileNo, string? email, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        TelephoneNo = telephoneNo?.Trim();
        MobileNo = mobileNo?.Trim();
        Email = email?.Trim();
        SetUpdatedInfo(now, updatedBy);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);
        DeletedBy = deletedBy;
        DeletedAt = now;
        IsDeleted = true;
    }
}
