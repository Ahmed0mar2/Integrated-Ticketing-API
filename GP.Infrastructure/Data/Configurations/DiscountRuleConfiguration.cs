using GP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class DiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
    {
        public void Configure(EntityTypeBuilder<DiscountRule> builder)
        {
            builder.ToTable("DiscountRules");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TargetTrips)
                .IsRequired();

            builder.Property(x => x.DiscountPercentage)
                .IsRequired()
                .HasPrecision(5, 2);

            builder.Property(x => x.MaxDiscountAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TargetTrips);
            builder.HasIndex(x => x.IsActive);
        }
    }
}
