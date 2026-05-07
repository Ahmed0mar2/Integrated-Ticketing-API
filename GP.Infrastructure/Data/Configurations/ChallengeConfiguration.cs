using GP.Domain.Entities;
using GP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
    {
        public void Configure(EntityTypeBuilder<Challenge> builder)
        {
            builder.ToTable("Challenges");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.GoalValue)
                .IsRequired();

            builder.Property(x => x.RewardPoints)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.Frequency)
                .IsRequired()
                .HasDefaultValue(ChallengeFrequency.Monthly);

            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Frequency);
        }
    }
}

