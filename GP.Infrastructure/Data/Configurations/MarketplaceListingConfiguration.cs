using GP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
    {
        public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OriginalPrice)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.AskingPrice)
                   .HasColumnType("decimal(18,2)");

            // Restrict delete: we don't want to delete a listing if it was already sold
            builder.HasOne(x => x.Seller)
                   .WithMany()
                   .HasForeignKey(x => x.SellerId)
                   .OnDelete(DeleteBehavior.Restrict);


            builder.HasOne(x => x.Booking)
                   .WithMany()
                   .HasForeignKey(x => x.BookingId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted && !x.Booking.IsDeleted);
        }
    }
}