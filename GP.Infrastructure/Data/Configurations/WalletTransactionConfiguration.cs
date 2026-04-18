using GP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.ToTable("WalletTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.BookingId);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.CreatedAt);

            builder.HasOne(x => x.User)
                .WithMany(u => u.WalletTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Booking)
                .WithMany(b => b.WalletTransactions)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        }
    }
}
