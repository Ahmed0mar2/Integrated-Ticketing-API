using GP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> builder)
        {
            builder.ToTable("SupportTickets");

            builder.HasKey(t => t.TicketId);

            builder.Property(t => t.UserId)
                .IsRequired();

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(t => t.Category)
                .IsRequired();

            builder.Property(t => t.Status)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(t => t.UpdatedAt)
                .IsRequired(false);

            builder.Property(t => t.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.Category);

            builder.HasOne(t => t.User)
                .WithMany(u => u.SupportTickets)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
