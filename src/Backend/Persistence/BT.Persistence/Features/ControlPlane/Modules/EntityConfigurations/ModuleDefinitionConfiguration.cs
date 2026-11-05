using BT.Domain.Features.ControlPlane.Modules.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BT.Persistence.Features.ControlPlane.Modules.EntityConfigurations;

internal sealed class ModuleDefinitionConfiguration : IEntityTypeConfiguration<ModuleDefinition>
{
    public void Configure(EntityTypeBuilder<ModuleDefinition> builder)
    {
        builder.ToTable("Modules");
        builder.HasKey(module => module.Id);
        builder.Property(module => module.Key).HasMaxLength(100).IsRequired();
        builder.Property(module => module.Name).HasMaxLength(160).IsRequired();
        builder.Property(module => module.Description).HasMaxLength(1000);
        builder.Property(module => module.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(module => module.UpdatedBy).HasMaxLength(128);
        builder.Property(module => module.DeletedBy).HasMaxLength(128);
        builder.Property(module => module.Revision).IsConcurrencyToken();
        builder.HasIndex(module => module.Key).IsUnique();
        builder.HasQueryFilter(module => !module.IsDeleted);
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            Seed("0194f700-0000-7000-8000-000000000101", "Banking", "Banking", createdAt),
            Seed("0194f700-0000-7000-8000-000000000102", "HR", "Human Resources", createdAt),
            Seed("0194f700-0000-7000-8000-000000000103", "Payments", "Payments", createdAt));
    }

    private static ModuleDefinition Seed(string id, string key, string name, DateTimeOffset createdAt) => new()
    {
        Id = Guid.Parse(id),
        Key = key,
        Name = name,
        IsActive = true,
        CreatedBy = "00000000-0000-0000-0000-000000000001",
        CreatedAt = createdAt,
        Revision = Guid.Parse(id)
    };
}
