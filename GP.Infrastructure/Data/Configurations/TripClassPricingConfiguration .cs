using GP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.Configurations
{
    public class TripClassPricingConfiguration : IEntityTypeConfiguration<TripClassPricing>
    {
        public void Configure(EntityTypeBuilder<TripClassPricing> builder)
        {
            builder.HasKey(t => t.TripClassPricingId);

            builder.Property(t => t.TripId).IsRequired();
            builder.Property(t => t.CoachClassId).IsRequired();
            builder.Property(t => t.PricingType).IsRequired();
            builder.Property(t => t.FullPrice).IsRequired().HasPrecision(10, 2);
            builder.Property(t => t.FullDistanceKm).IsRequired(false).HasPrecision(10, 2);  
            builder.Property(t => t.MinimumPrice).IsRequired(false).HasPrecision(10, 2);
            builder.Property(t => t.RoundingStep).IsRequired(false);

            // Composite unique: each trip+class has exactly one pricing config
            builder.HasIndex(t => new { t.TripId, t.CoachClassId }).IsUnique();
            builder.HasIndex(t => t.TripId);
            builder.HasOne(t => t.Trip).WithMany(tr => tr.TripClassPricings).HasForeignKey(t => t.TripId);
            builder.HasOne(t => t.CoachClass).WithMany(c => c.PricingConfigs).HasForeignKey(t => t.CoachClassId);
        }
    }
}
