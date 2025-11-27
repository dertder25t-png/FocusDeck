using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}

public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Picture).HasMaxLength(500);
        builder.Property(u => u.CreatedAt).IsRequired();
    }
}

public class UserTenantConfiguration : IEntityTypeConfiguration<UserTenant>
{
    public void Configure(EntityTypeBuilder<UserTenant> builder)
    {
        builder.HasKey(ut => ut.Id);

        builder.HasOne(ut => ut.Tenant)
            .WithMany(t => t.Members)
            .HasForeignKey(ut => ut.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ut => ut.User)
            .WithMany(u => u.Tenants)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ut => new { ut.TenantId, ut.UserId }).IsUnique();
        builder.Property(ut => ut.Role).IsRequired();
        builder.Property(ut => ut.JoinedAt).IsRequired();
    }
}

public class TenantInviteConfiguration : IEntityTypeConfiguration<TenantInvite>
{
    public void Configure(EntityTypeBuilder<TenantInvite> builder)
    {
        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Tenant)
            .WithMany(t => t.Invites)
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.Email).IsRequired().HasMaxLength(255);
        builder.Property(i => i.Token).IsRequired().HasMaxLength(100);
        builder.HasIndex(i => i.Token).IsUnique();
        builder.Property(i => i.Role).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();
    }
}
