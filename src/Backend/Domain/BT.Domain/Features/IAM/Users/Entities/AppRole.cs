using BT.Domain.Shared.Contracts.Common;
using Microsoft.AspNetCore.Identity;

namespace BT.Domain.Features.IAM.Users.Entities;

public sealed class AppRole : IdentityRole, ISoftDeletable, IOptionalTenantScope
{
    public AppRole() => Id = Guid.CreateVersion7().ToString();
    public Guid TenantId { get; set; }
    public bool IsDefault { get; set; }
    bool IOptionalTenantScope.IsGlobalDefinition => IsDefault;
    public bool IsPlatformRole { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ModuleKey { get; set; }
    public string? DefinitionKey { get; set; }
    public Guid? DepartmentId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
    }
}
