using BT.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BT.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AspNetRoles");

        builder.Property(role => role.DepartmentId);
        builder.Property(role => role.ModuleKey).HasMaxLength(100);
        builder.Property(role => role.DefinitionKey).HasMaxLength(80);
        builder.HasIndex(role => role.DefinitionKey).IsUnique().HasFilter("[DefinitionKey] IS NOT NULL");
        builder.Property(role => role.IsActive).HasDefaultValue(true);
        builder.HasIndex(role => role.NormalizedName).IsUnique(false).HasDatabaseName("RoleNameIndex");
        builder.HasIndex(role => new { role.TenantId, role.NormalizedName }).IsUnique();

        builder.Property(role => role.DeletedBy)
            .HasMaxLength(100);

        builder.HasIndex(role => role.IsDeleted)
            .HasDatabaseName("IX_AspNetRoles_IsDeleted");

        builder.HasIndex(role => role.DepartmentId)
            .HasDatabaseName("IX_AspNetRoles_DepartmentId");
    }
}
