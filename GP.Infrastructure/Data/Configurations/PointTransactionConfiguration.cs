using GP.Domain.Entities;
using GP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
    {
        public void Configure(EntityTypeBuilder<PointTransaction> builder)
        {
            builder.ToTable("PointTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Amount)
                .IsRequired();

            builder.Property(x => x.AvailableAmount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.ParentTransactionId)
                .IsRequired(false);

            builder.Property(x => x.Source)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.UnlocksAt)
                .IsRequired(false);

            builder.Property(x => x.ExpiresAt)
                .IsRequired(false);

            builder.Property(x => x.IsExpired)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.BookingId);
            builder.HasIndex(x => x.ParentTransactionId);
            builder.HasIndex(x => x.Source);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => new { x.IsExpired, x.ExpiresAt });

            builder.HasOne(x => x.User)
                .WithMany(u => u.PointTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Booking)
                .WithMany(b => b.PointTransactions)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.HasOne(x => x.ParentTransaction)
                .WithMany()
                .HasForeignKey(x => x.ParentTransactionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
