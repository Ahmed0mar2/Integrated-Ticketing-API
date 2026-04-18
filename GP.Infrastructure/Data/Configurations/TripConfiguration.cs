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
    public class TripConfiguration : IEntityTypeConfiguration<Trip>
    {
        public void Configure(EntityTypeBuilder<Trip> builder)
        {
            builder.HasKey(t => t.TripId);

            builder.Property(t => t.AgencyId).IsRequired();
            builder.Property(t => t.OriginStationId).IsRequired();
            builder.Property(t => t.DestinationStationId).IsRequired();
            builder.Property(t => t.ServiceId).IsRequired();

            // Nullable for Horus and TripCode
            builder.Property(t => t.TotalDurationMinutes).IsRequired(false);
            builder.Property(t => t.TripCode).IsRequired(false).HasMaxLength(100);

            builder.Property(t => t.DepartureTime).IsRequired().HasColumnType("time");

            // Indexes
            builder.HasIndex(t => new { t.OriginStationId, t.DestinationStationId, t.DepartureTime })
                .HasDatabaseName("IX_Trips_Search_RouteTime");
            builder.HasIndex(t => new { t.OriginStationId, t.DestinationStationId });

            // Relationships (RESTRICT to prevent cascade crashes)
            builder.HasOne(t => t.OriginStation)
                   .WithMany()
                   .HasForeignKey(t => t.OriginStationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.DestinationStation)
                   .WithMany()
                   .HasForeignKey(t => t.DestinationStationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Agency).WithMany(a => a.Trips).HasForeignKey(t => t.AgencyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(t => t.Calendar).WithMany(c => c.Trips).HasForeignKey(t => t.ServiceId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
