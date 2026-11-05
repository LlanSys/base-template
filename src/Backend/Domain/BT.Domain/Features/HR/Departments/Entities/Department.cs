using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;

namespace BT.Domain.Features.HR.Departments.Entities;

public sealed class Department : BaseEntity, ISoftDeletable
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private Department()
    {
    }

    public static Department Create(string code, string name, string description, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new Department
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description.Trim(),
            CreatedBy = createdBy.Trim()
        };
    }

    public void Update(DateTimeOffset now, string code, string name, string description, bool isActive, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsActive = isActive;
        SetUpdatedInfo(now, updatedBy);
    }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        IsActive = false;
        DeletedAt = now;
        DeletedBy = deletedBy.Trim();
        SetUpdatedInfo(now, deletedBy);
    }
}
