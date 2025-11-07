using FocusDeck.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusDeck.Persistence.Configurations;

public class AuthEventLogConfiguration : IEntityTypeConfiguration<AuthEventLog>
{
    public void Configure(EntityTypeBuilder<AuthEventLog> builder)
    {
        builder.ToTable("AuthEventLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.UserId).HasMaxLength(320);
        builder.Property(x => x.RemoteIp).HasMaxLength(64);
        builder.Property(x => x.DeviceId).HasMaxLength(128);
        builder.Property(x => x.DeviceName).HasMaxLength(256);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.FailureReason).HasMaxLength(256);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}

