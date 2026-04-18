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
    public class TripOccurrenceClassInventoryConfiguration: IEntityTypeConfiguration<TripOccurrenceClassInventory>
    {
        public void Configure(EntityTypeBuilder<TripOccurrenceClassInventory> builder)
        {
            builder.HasKey(t => t.TripOccurrenceClassInventoryId);

            builder.Property(t => t.TripOccurrenceId).IsRequired();
            builder.Property(t => t.CoachClassId).IsRequired();
            builder.Property(t => t.TotalSeats).IsRequired();
            builder.Property(t => t.RemainingSeats).IsRequired();
            builder.Property(t => t.RowVersion).IsRowVersion();

            // Indexes
            builder.HasIndex(t => t.TripOccurrenceId);
            builder.HasIndex(t => t.CoachClassId);
            builder.HasIndex(t => new { t.RemainingSeats }).IsDescending();
            builder.HasIndex(i => new { i.TripOccurrenceId, i.CoachClassId, i.RemainingSeats });
            builder.HasIndex(t => new { t.TripOccurrenceId, t.CoachClassId }).IsUnique();

            // Constraints
            builder.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_RemainingSeatsValid",
                    "[RemainingSeats] <= [TotalSeats] AND [RemainingSeats] >= 0");
            });

            // Relationships
            builder.HasOne(t => t.TripOccurrence)
                .WithMany(to => to.ClassInventories)
                .HasForeignKey(t => t.TripOccurrenceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.CoachClass)
                .WithMany(c => c.Inventories)  
                .HasForeignKey(t => t.CoachClassId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
