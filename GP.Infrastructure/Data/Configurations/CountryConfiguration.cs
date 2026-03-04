namespace GP.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GP.Domain.Entities;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");

        builder.HasKey(c => c.CountryId);

        builder.Property(c => c.CountryId)
            .HasColumnName("country_id");

        builder.Property(c => c.CountryCode)
            .HasColumnName("country_code")
            .IsRequired()
            .HasMaxLength(2); // ISO Alpha-2

        builder.Property(c => c.CountryName)
            .HasColumnName("country_name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.NationalityName)
            .HasColumnName("nationality_name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.PhoneCode)
            .HasColumnName("phone_code")
            .HasMaxLength(10);

        builder.Property(c => c.AllowsTrainBooking)
            .HasColumnName("allows_train_booking")
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(c => c.CountryCode)
            .IsUnique();

        builder.HasIndex(c => c.CountryName);
    }
}