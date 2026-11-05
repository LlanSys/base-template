using BT.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BT.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class SsoAuthorizationCodeConfiguration : IEntityTypeConfiguration<SsoAuthorizationCode>
{
    public void Configure(EntityTypeBuilder<SsoAuthorizationCode> builder)
    {
        builder.ToTable("SsoAuthorizationCodes");
        builder.HasKey(code => code.Hash);
        builder.Property(code => code.Hash).HasMaxLength(64);
        builder.Property(code => code.UserId).HasMaxLength(450);
        builder.HasIndex(code => code.ExpiresAt);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(code => code.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
