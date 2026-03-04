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
    public class TripOccurrenceConfiguration : IEntityTypeConfiguration<TripOccurrence>
    {
        public void Configure(EntityTypeBuilder<TripOccurrence> builder)
        {
            builder.HasKey(t => t.TripOccurrenceId);

            builder.Property(t => t.TripId).IsRequired();
            builder.Property(t => t.OccurrenceDate).IsRequired();
            builder.Property(t => t.DepartureDateTime).IsRequired();
            builder.Property(t => t.ArrivalDateTime).IsRequired();
            builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);

            // Indexes - Critical for 30-day window queries
            builder.HasIndex(t => new { t.TripId, t.OccurrenceDate }).IsUnique();
            builder.HasIndex(t => t.OccurrenceDate);
            builder.HasIndex(t => new { t.OccurrenceDate, t.IsActive });

            // Relationships
            builder.HasOne(t => t.Trip)
                .WithMany(tr => tr.TripOccurrences)
                .HasForeignKey(t => t.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.ClassInventories)
                .WithOne(ci => ci.TripOccurrence)
                .HasForeignKey(ci => ci.TripOccurrenceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
