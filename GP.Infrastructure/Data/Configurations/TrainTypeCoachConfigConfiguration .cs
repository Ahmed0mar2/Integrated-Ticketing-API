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
    public class TrainTypeCoachConfigConfiguration : IEntityTypeConfiguration<TrainTypeCoachConfig>
    {
        public void Configure(EntityTypeBuilder<TrainTypeCoachConfig> builder)
        {
            builder.HasKey(t => t.TrainTypeCoachConfigId);

            builder.HasIndex(t => new { t.TrainTypeId, t.CoachClassId }).IsUnique();

            builder.Property(t => t.NumberOfCoaches).IsRequired();
            builder.Property(t => t.SeatsPerCoach).IsRequired();

            builder.HasOne(t => t.TrainType).WithMany(tt => tt.CoachConfigs).HasForeignKey(t => t.TrainTypeId);
            builder.HasOne(t => t.CoachClass).WithMany(c => c.TrainTypeConfigs).HasForeignKey(t => t.CoachClassId);
        }
    }
}
