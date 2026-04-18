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
   public class TripFareConfiguration : IEntityTypeConfiguration<TripFare>
    {
        public void Configure(EntityTypeBuilder<TripFare> builder)
        {
            builder.HasKey(tf => tf.TripFareId);
    
            builder.Property(tf => tf.Price).IsRequired().HasPrecision(10, 2);
    
            // Unique Matrix Index
            builder.HasIndex(tf => new { tf.TripId, tf.OriginStationId, tf.DestinationStationId, tf.CoachClassId })
                   .IsUnique();
            builder.HasIndex(f => new { f.TripId, f.OriginStationId, f.DestinationStationId, f.CoachClassId });

            builder.HasOne(tf => tf.Trip).WithMany(t => t.TripFares).HasForeignKey(tf => tf.TripId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(tf => tf.CoachClass).WithMany(c => c.PricingConfigs).HasForeignKey(tf => tf.CoachClassId).OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(tf => tf.OriginStation).WithMany().HasForeignKey(tf => tf.OriginStationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(tf => tf.DestinationStation).WithMany().HasForeignKey(tf => tf.DestinationStationId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
