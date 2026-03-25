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
    public class CoachClassConfiguration : IEntityTypeConfiguration<CoachClass>
    {
        public void Configure(EntityTypeBuilder<CoachClass> builder)
        {
            builder.HasKey(c => c.CoachClassId);
            builder.Property(c => c.CoachClassId).ValueGeneratedNever();
            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.HasIndex(c => c.Name).IsUnique();
            builder.HasMany(c => c.PricingConfigs).WithOne(p => p.CoachClass).HasForeignKey(p => p.CoachClassId);
            builder.HasMany(c => c.Inventories).WithOne(i => i.CoachClass).HasForeignKey(i => i.CoachClassId);
            builder.Property(c => c.CoachClassId)
                   .ValueGeneratedOnAdd();

        }
    }
}
