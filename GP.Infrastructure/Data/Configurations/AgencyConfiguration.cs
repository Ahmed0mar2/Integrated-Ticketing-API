using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GP.Domain.Entities;

namespace GP.Infrastructure.Data.Configurations
{
    public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
    {
        public void Configure(EntityTypeBuilder<Agency> builder)
        {
            builder.HasKey(a => a.AgencyId);
            builder.Property(a => a.AgencyName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.AgencyType).IsRequired();
            
            builder.HasIndex(a => a.AgencyName).IsUnique();
            builder.HasMany(a => a.Trips).WithOne(t => t.Agency).HasForeignKey(t => t.AgencyId);
            builder.Property(a => a.AgencyId)
                   .ValueGeneratedOnAdd();
        }
    }
}
