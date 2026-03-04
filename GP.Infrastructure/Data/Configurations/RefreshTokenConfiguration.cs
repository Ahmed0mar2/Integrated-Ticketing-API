using GP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");

            builder.HasKey(rt => rt.TokenId);

            builder.Property(rt => rt.TokenId)
                .HasColumnName("token_id");

            builder.Property(rt => rt.ApplicationUserId)
                .HasColumnName("application_user_id");

            builder.Property(rt => rt.TokenHash)
                .HasColumnName("token_hash")
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(rt => rt.DeviceInfo)
                .HasColumnName("device_info")
                .HasMaxLength(200);

            builder.Property(rt => rt.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(50);

            builder.Property(rt => rt.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(rt => rt.ExpiresAt)
                .HasColumnName("expires_at");

            builder.Property(rt => rt.IsRevoked)
                .HasColumnName("is_revoked")
                .HasDefaultValue(false);

            builder.Property(rt => rt.RevokedAt)
                .HasColumnName("revoked_at");

            builder.Property(rt => rt.RevokedByIp)
                .HasColumnName("revoked_by_ip")
                .HasMaxLength(50);

            builder.HasOne(rt => rt.ApplicationUser)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rt => rt.ApplicationUserId);
            builder.HasIndex(rt => rt.TokenHash);
            builder.HasIndex(rt => rt.ExpiresAt);

            builder.HasIndex(rt => new { rt.ApplicationUserId, rt.IsRevoked }); 
        }
    }
}
