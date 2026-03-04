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
    public class TrainTypeConfiguration : IEntityTypeConfiguration<TrainType>
    {
        public void Configure(EntityTypeBuilder<TrainType> builder)
        {
            builder.HasKey(t => t.TrainTypeId);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

            builder.HasIndex(t => t.Name).IsUnique();
            builder.HasMany(t => t.CoachConfigs).WithOne(c => c.TrainType).HasForeignKey(c => c.TrainTypeId);
        }
    }

}
