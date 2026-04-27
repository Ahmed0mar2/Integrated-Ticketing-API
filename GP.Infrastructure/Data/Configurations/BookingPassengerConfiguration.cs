using GP.Domain.Entities;
using GP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
    {
        public void Configure(EntityTypeBuilder<BookingPassenger> builder)
        {
            builder.HasKey(bp => bp.PassengerId);

            // Required fields
            builder.Property(bp => bp.BookingId).IsRequired();
            builder.Property(bp => bp.Name).IsRequired().HasMaxLength(200);
            builder.Property(bp => bp.SeatNumber).IsRequired().HasMaxLength(50);
            builder.Property(bp => bp.IdType).IsRequired(false);
            builder.Property(bp => bp.IdNumber).IsRequired(false).HasMaxLength(50);

            // Shadow copy of parent booking status used by filtered unique index enforcement.
            builder.Property<int>("BookingStatus")
                .IsRequired()
                .HasDefaultValue((int)BookingStatus.Pending);

            // Audit fields
            builder.Property(bp => bp.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            builder.Property(bp => bp.UpdatedAt).IsRequired(false);
            builder.Property(bp => bp.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(bp => bp.IsOfferedForResale).IsRequired().HasDefaultValue(false);

            // Indexes
            builder.HasIndex(bp => bp.BookingId);
            builder.HasIndex(bp => new { bp.BookingId, bp.PassengerId });
            builder.HasIndex(bp => new { bp.OccurrenceId, bp.CoachClassId, bp.SeatNumber })
                   .IsUnique()
                   .HasDatabaseName("IX_BookingPassenger_UniqueSeat");
            builder.HasIndex("OccurrenceId", "IdNumber")
                .IsUnique()
                .HasDatabaseName("IX_BookingPassenger_UniquePassengerPerOccurrence_Active")
                .HasFilter($"[BookingStatus] IN ({(int)BookingStatus.Pending}, {(int)BookingStatus.Confirmed}) AND [IdNumber] IS NOT NULL");

            // Relationship
            builder.HasOne(bp => bp.Booking)
                .WithMany(b => b.BookingPassengers)
                .HasForeignKey(bp => bp.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table metadata: SQL trigger registration
            builder.ToTable(tb =>
            {
                tb.HasTrigger("TR_BookingPassengers_SyncBookingStatus");
            });
        }
    }
}
