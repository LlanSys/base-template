using BT.Domain.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BT.Persistence.Common;

internal static class AdministrationAuditCollector
{
    private static readonly HashSet<string> AuditedTypes = new(StringComparer.Ordinal)
    {
        "Tenant", "TenantModule", "TenantAdministratorInvitation", "DeploymentStamp", "ModuleDefinition", "AppRole", "Permission", "MenuItem",
        "AppUser", "IdentityUserRole<string>", "IdentityRoleClaim<string>", "IdentityUserClaim<string>", "IdentityUserLogin<string>", "OrgSetting"
    };

    public static IReadOnlyList<AdministrationAuditEntry> Collect(ChangeTracker tracker, string actorId, Guid tenantId, DateTimeOffset now)
    {
        var result = new List<AdministrationAuditEntry>();

        foreach (var entry in tracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
        {
            var type = entry.Metadata.ClrType;
            var name = type.IsGenericType ? type.GetGenericTypeDefinition().Name.Split('`')[0] + "<string>" : type.Name;

            if (!AuditedTypes.Contains(name)) continue;
            var subject = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "UserId")
                ?? entry.Properties.FirstOrDefault(property => property.Metadata.Name == "RoleId")
                ?? entry.Properties.FirstOrDefault(property => property.Metadata.Name == "Id");

            var owner = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "TenantId")?.CurrentValue as Guid?;
            result.Add(new AdministrationAuditEntry
            {
                TenantId = owner ?? (tenantId == Guid.Empty ? null : tenantId),
                ActorId = actorId,
                OccurredAt = now,
                EntityType = name,
                EntityId = subject?.CurrentValue?.ToString() ?? string.Empty,
                Action = entry.State == EntityState.Deleted || entry.Properties.Any(property => property.Metadata.Name == "IsDeleted" && property.CurrentValue is true)
                    ? "Delete" : entry.State == EntityState.Added ? "Create" : "Update",
                ChangedFields = string.Join(", ", entry.Properties.Where(property => property.IsModified || entry.State == EntityState.Added)
                    .Select(property => property.Metadata.Name).Order(StringComparer.Ordinal))
            });
        }
        return result;
    }
}
