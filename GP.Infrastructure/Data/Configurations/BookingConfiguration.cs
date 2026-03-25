using GP.Domain.Entities;
using GP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.BookingId);

            // Required fields
            builder.Property(b => b.UserId).IsRequired();
            builder.Property(b => b.OccurrenceId).IsRequired();  
            builder.Property(b => b.CoachClassId).IsRequired();  
            builder.Property(b => b.SeatsBooked).IsRequired();
            builder.Property(b => b.TotalPrice).IsRequired().HasPrecision(10, 2);
            builder.Property(b => b.BookingTime).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            builder.Property(b => b.Status).IsRequired();
            builder.Property(b => b.PaymentStatus).IsRequired();

            // Audit fields
            builder.Property(b => b.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            builder.Property(b => b.UpdatedAt).IsRequired(false);
            builder.Property(b => b.IsDeleted).IsRequired().HasDefaultValue(false);

            // Indexes for quick lookups
            builder.HasIndex(b => b.UserId);
            builder.HasIndex(b => b.OccurrenceId);
            builder.HasIndex(b => b.Status);
            builder.HasIndex(b => b.PaymentStatus);

            // Relationships
            builder.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);  

            builder.HasOne(b => b.Occurrence)
                .WithMany()
                .HasForeignKey(b => b.OccurrenceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.CoachClass)
                .WithMany()
                .HasForeignKey(b => b.CoachClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(b => b.BookingPassengers)
                .WithOne(bp => bp.Booking)
                .HasForeignKey(bp => bp.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(b => b.OriginStation)
                    .WithMany()
                    .HasForeignKey(b => b.OriginStationId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.DestinationStation)
                   .WithMany()
                   .HasForeignKey(b => b.DestinationStationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
