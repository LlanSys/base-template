using BT.Domain.Shared.Contracts.Common;

namespace BT.Domain.Features.ControlPlane.Modules.Entities;

public sealed class ModuleDefinition : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public Guid Revision { get; set; } = Guid.CreateVersion7();

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);
        IsDeleted = true;
        IsActive = false;
        DeletedBy = deletedBy;
        DeletedAt = now;
        Revision = Guid.CreateVersion7();
    }
}
