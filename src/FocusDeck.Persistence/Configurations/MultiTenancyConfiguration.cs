using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(o => o.Slug).IsUnique();
        builder.Property(o => o.CreatedAt).IsRequired();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Picture).HasMaxLength(500);
        builder.Property(u => u.CreatedAt).IsRequired();
    }
}

public class OrgUserConfiguration : IEntityTypeConfiguration<OrgUser>
{
    public void Configure(EntityTypeBuilder<OrgUser> builder)
    {
        builder.HasKey(ou => ou.Id);
        
        builder.HasOne(ou => ou.Organization)
            .WithMany(o => o.Members)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(ou => ou.User)
            .WithMany(u => u.Organizations)
            .HasForeignKey(ou => ou.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(ou => new { ou.OrganizationId, ou.UserId }).IsUnique();
        builder.Property(ou => ou.Role).IsRequired();
        builder.Property(ou => ou.JoinedAt).IsRequired();
    }
}

public class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.HasKey(i => i.Id);
        
        builder.HasOne(i => i.Organization)
            .WithMany(o => o.Invites)
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Property(i => i.Email).IsRequired().HasMaxLength(255);
        builder.Property(i => i.Token).IsRequired().HasMaxLength(100);
        builder.HasIndex(i => i.Token).IsUnique();
        builder.Property(i => i.Role).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();
    }
}
