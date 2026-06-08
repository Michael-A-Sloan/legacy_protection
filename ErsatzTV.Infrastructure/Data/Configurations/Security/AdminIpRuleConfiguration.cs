using ErsatzTV.Core.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations.Security;

public class AdminIpRuleConfiguration : IEntityTypeConfiguration<AdminIpRule>
{
    public void Configure(EntityTypeBuilder<AdminIpRule> builder)
    {
        builder.ToTable("AdminIpRule");
        builder.HasIndex(r => new { r.IpAddress, r.RuleType }).IsUnique();
        builder.Property(r => r.IpAddress).IsRequired();
        builder.Property(r => r.Note).HasDefaultValue(string.Empty);
        builder.Property(r => r.BlockIptvStreaming).HasDefaultValue(false);
    }
}
