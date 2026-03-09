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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.UserId);

            builder.Property(u => u.UserId)
                .HasColumnName("user_id");

            builder.Property(u => u.FirstName)
                .HasColumnName("first_name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .HasColumnName("last_name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.FamilyName)
                .HasColumnName("family_name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .HasColumnName("email")
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.Phone)
                .HasColumnName("phone")
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(u => u.Gender)
                .HasColumnName("gender")
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(u => u.DateOfBirth)
                .HasColumnName("date_of_birth");

            builder.Property(u => u.NationalIdNumber)
                .HasColumnName("national_id_number")
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(u => u.CountryId)
               .HasColumnName("country_id");

            builder.HasOne(u => u.Country)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(u => u.IsNationalIdVerified)
                .HasColumnName("is_national_id_verified")
                .HasDefaultValue(false);

            builder.Property(u => u.ProfilePictureUrl)
                .HasColumnName("profile_picture_url")
                .HasMaxLength(500);

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.HasIndex(u => u.NationalIdNumber)
            .IsUnique()
            .HasFilter("[national_id_number] IS NOT NULL");

            builder.HasIndex(u => u.Phone);

            builder.Property(u => u.CurrentCity)
            .HasColumnName("current_city")
            .HasMaxLength(100)
            .IsRequired(false);

            builder.Property(u => u.Latitude)
                .HasColumnName("latitude")
                .HasPrecision(9, 6)
                .IsRequired(false);

            builder.Property(u => u.Longitude)
                .HasColumnName("longitude")
                .HasPrecision(9, 6)
                .IsRequired(false);

            
            builder.Property(u => u.TotalTripsCount)
                .HasColumnName("total_trips_count")
                .HasDefaultValue(0);

            builder.Property(u => u.TotalDistanceTraveled)
                .HasColumnName("total_distance_traveled")
                .HasPrecision(10, 2)
                .HasDefaultValue(0);

            builder.Property(u => u.WalletBalance)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);

            builder.HasIndex(u => u.CurrentCity);

            builder.HasIndex(u => u.CountryId);

        }
    }
}
