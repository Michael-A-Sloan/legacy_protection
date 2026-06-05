using ErsatzTV.Core.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Security;

public class AdminLoginAttemptConfiguration : IEntityTypeConfiguration<AdminLoginAttempt>
{
    public void Configure(EntityTypeBuilder<AdminLoginAttempt> builder)
    {
        builder.ToTable("AdminLoginAttempt");
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.IpAddress);
        builder.Property(a => a.IpAddress).IsRequired();
        builder.Property(a => a.IpAddressV4).HasDefaultValue(string.Empty);
        builder.Property(a => a.IpAddressV6).HasDefaultValue(string.Empty);
        builder.Property(a => a.Username).HasDefaultValue(string.Empty);
        builder.Property(a => a.DenyReason).HasDefaultValue(string.Empty);
        builder.Property(a => a.UserAgent).HasDefaultValue(string.Empty);
        builder.Property(a => a.AttemptKind).HasDefaultValue(AdminLoginAttemptKind.Login);
        builder.Property(a => a.RequestPath).HasDefaultValue(string.Empty);
    }
}
