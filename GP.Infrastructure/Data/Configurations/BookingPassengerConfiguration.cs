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
    public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
    {
        public void Configure(EntityTypeBuilder<BookingPassenger> builder)
        {
            builder.HasKey(bp => bp.PassengerId);

            // Required fields
            builder.Property(bp => bp.BookingId).IsRequired();
            builder.Property(bp => bp.Name).IsRequired().HasMaxLength(200);
            builder.Property(bp => bp.Age).IsRequired();
            builder.Property(bp => bp.SeatNumber).IsRequired().HasMaxLength(50); 
            builder.Property(bp => bp.IdType).IsRequired();
            builder.Property(bp => bp.IdNumber).IsRequired().HasMaxLength(50);

            // Audit fields 
            builder.Property(bp => bp.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            builder.Property(bp => bp.UpdatedAt).IsRequired(false);
            builder.Property(bp => bp.IsDeleted).IsRequired().HasDefaultValue(false);

            // Indexes
            builder.HasIndex(bp => bp.BookingId);
            builder.HasIndex(bp => new { bp.BookingId, bp.PassengerId });

            // Relationship
            builder.HasOne(bp => bp.Booking)
                .WithMany(b => b.BookingPassengers)
                .HasForeignKey(bp => bp.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Constraint: Age >= 0
            builder.ToTable(t => t.HasCheckConstraint("CK_ValidAge", "[Age] >= 0"));
        }
    }
}
