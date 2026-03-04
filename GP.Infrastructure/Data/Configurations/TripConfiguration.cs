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

            // Primary Blueprint Fields
            builder.Property(t => t.AgencyId).IsRequired();
            builder.Property(t => t.OriginStationId).IsRequired();
            builder.Property(t => t.DestinationStationId).IsRequired();
            builder.Property(t => t.TotalDurationMinutes).IsRequired();
            builder.Property(t => t.ServiceId).IsRequired();
            builder.Property(t => t.TotalSeats).IsRequired();

            builder.Property(t => t.DepartureTime)
                .IsRequired()
                .HasColumnType("time"); 

            builder.Property(t => t.TrainTypeId).IsRequired(false);
            builder.Property(t => t.ServiceClass).IsRequired(false).HasMaxLength(100);
            builder.Property(t => t.BasePrice).IsRequired(false).HasPrecision(10, 2);

            // 2. Critical Search Indexes
            // Composite index for the Search Service: Route + Time
            builder.HasIndex(t => new { t.OriginStationId, t.DestinationStationId, t.DepartureTime })
                .HasDatabaseName("IX_Trips_Search_RouteTime");

            builder.HasIndex(t => t.ServiceId);
            builder.HasIndex(t => t.AgencyId);

            // 3. Relationships
            builder.HasOne(t => t.Agency)
                .WithMany(a => a.Trips)
                .HasForeignKey(t => t.AgencyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Calendar)
                .WithMany(c => c.Trips)
                .HasForeignKey(t => t.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.TrainType)
                .WithMany() 
                .HasForeignKey(t => t.TrainTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
