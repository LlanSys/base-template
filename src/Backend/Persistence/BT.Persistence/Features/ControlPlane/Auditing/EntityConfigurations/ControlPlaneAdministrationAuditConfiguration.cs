using BT.Domain.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BT.Persistence.Features.ControlPlane.Auditing.EntityConfigurations;

internal sealed class ControlPlaneAdministrationAuditConfiguration : IEntityTypeConfiguration<AdministrationAuditEntry>
{
    public void Configure(EntityTypeBuilder<AdministrationAuditEntry> builder)
    {
        builder.ToTable("ControlPlaneAdministrationAudit");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ActorId).HasMaxLength(128).IsRequired();
        builder.Property(entry => entry.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(entry => entry.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(20).IsRequired();
        builder.Property(entry => entry.ChangedFields).HasMaxLength(2000).IsRequired();
        builder.HasIndex(entry => new { entry.TenantId, entry.OccurredAt });
    }
}
