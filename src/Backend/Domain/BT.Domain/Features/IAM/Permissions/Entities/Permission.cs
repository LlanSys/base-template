using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;

namespace BT.Domain.Features.IAM.Permissions.Entities;

public sealed class Permission : BaseEntity, ISoftDeletable, IPlatformCatalogEntity
{
    public Guid? DepartmentId { get; private set; }
    public Guid? AvailableTenantId { get; set; }
    public string? RequiredModule { get; set; }
    public bool IsTenantDelegatable { get; set; }
    public string Key { get; private set; } = string.Empty;
    public string Context { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private Permission()
    {
    }

    public static Permission Create(
        Guid? departmentId,
        string context,
        string resource,
        string action,
        string description,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var normalizedResource = NormalizeSegment(resource);
        var normalizedAction = NormalizeSegment(action);

        return new Permission
        {
            Id = Guid.CreateVersion7(),
            DepartmentId = departmentId,
            Context = context.Trim(),
            Resource = normalizedResource,
            Action = normalizedAction,
            Key = $"{normalizedResource}.{normalizedAction}",
            Description = description.Trim(),
            CreatedBy = createdBy.Trim()
        };
    }

    public void Update(DateTimeOffset now, Guid? departmentId, string context, string resource, string action, string description, bool isActive, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        var normalizedResource = NormalizeSegment(resource);
        var normalizedAction = NormalizeSegment(action);

        DepartmentId = departmentId;
        Context = context.Trim();
        Resource = normalizedResource;
        Action = normalizedAction;
        Key = $"{normalizedResource}.{normalizedAction}";
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

    private static string NormalizeSegment(string value)
        => value.Trim().ToLowerInvariant().Replace(' ', '_');
}
