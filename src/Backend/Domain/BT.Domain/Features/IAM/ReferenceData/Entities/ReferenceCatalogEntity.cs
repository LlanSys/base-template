using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;

namespace BT.Domain.Features.IAM.ReferenceData.Entities;

public abstract class ReferenceCatalogEntity : BaseEntity, ISoftDeletable, IPlatformCatalogEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy.Trim();
    }
}
