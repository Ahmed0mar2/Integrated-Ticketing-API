using GP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GP.Infrastructure.Data.Configurations
{
    public class UserChallengeConfiguration : IEntityTypeConfiguration<UserChallenge>
    {
        public void Configure(EntityTypeBuilder<UserChallenge> builder)
        {
            builder.ToTable("UserChallenges");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.ChallengeId)
                .IsRequired();

            builder.Property(x => x.CurrentProgress)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.IsCompleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.CompletedAt)
                .IsRequired(false);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.ChallengeId);
            builder.HasIndex(x => x.IsCompleted);

            builder.HasOne(x => x.User)
                .WithMany(u => u.UserChallenges)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Challenge)
                .WithMany(c => c.UserChallenges)
                .HasForeignKey(x => x.ChallengeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
