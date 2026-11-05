using BT.Domain.Features.ControlPlane.Tenants.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BT.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class TenantAdministratorInvitationConfiguration : IEntityTypeConfiguration<TenantAdministratorInvitation>
{
    public void Configure(EntityTypeBuilder<TenantAdministratorInvitation> builder)
    {
        builder.ToTable("TenantAdministratorInvitations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Email).HasMaxLength(256).IsRequired();
        builder.Property(item => item.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(item => item.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(item => item.LastName).HasMaxLength(100).IsRequired();
        builder.Property(item => item.LastFailure).HasMaxLength(200);
        builder.Property(item => item.TokenHash).HasMaxLength(64);
        builder.Property(item => item.ClaimedUserId).HasMaxLength(450);
        builder.Property(item => item.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(item => item.UpdatedBy).HasMaxLength(128);
        builder.Property(item => item.Revision).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.NormalizedEmail }).IsUnique();
        builder.HasIndex(item => new { item.Status, item.NextAttemptAt });
        builder.HasOne(item => item.Tenant).WithMany(tenant => tenant.AdministratorInvitations).HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
