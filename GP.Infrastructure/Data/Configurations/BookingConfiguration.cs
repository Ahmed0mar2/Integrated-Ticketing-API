using GP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
            builder.Property(b => b.ContactName).IsRequired().HasMaxLength(200);
            builder.Property(b => b.ContactPhone).IsRequired().HasMaxLength(50);
            builder.Property(b => b.ContactEmail).IsRequired().HasMaxLength(255);

            // Audit fields
            builder.Property(b => b.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            builder.Property(b => b.UpdatedAt).IsRequired(false);
            builder.Property(b => b.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(b => b.HoldExpiresAt).IsRequired(false);

            // Indexes for quick lookups
            builder.HasIndex(b => b.UserId);
            builder.HasIndex(b => b.OccurrenceId);
            builder.HasIndex(b => b.Status);
            builder.HasIndex(b => b.PaymentStatus);
            // Speeds up the background worker that clears expired carts
            builder.HasIndex(b => new { b.Status, b.HoldExpiresAt });

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

            builder.HasMany(b => b.WalletTransactions)
                .WithOne(w => w.Booking)
                .HasForeignKey(w => w.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // SQL trigger registration (created in AddActivePassengerDuplicateGuard migration)
            builder.ToTable(tb => tb.HasTrigger("TR_Bookings_PropagateStatusToPassengers"));
        }
    }
}
