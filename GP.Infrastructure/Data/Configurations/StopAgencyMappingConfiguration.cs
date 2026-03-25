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
    public class StopAgencyMappingConfiguration : IEntityTypeConfiguration<StopAgencyMapping>
    {
        public void Configure(EntityTypeBuilder<StopAgencyMapping> builder)
        {
            builder.HasKey(m => m.StopAgencyMappingId);
            builder.Property(sam => sam.StopAgencyMappingId).ValueGeneratedOnAdd();

            builder.Property(m => m.ExternalStationId).IsRequired().HasMaxLength(100);

            builder.HasIndex(m => new { m.AgencyId, m.ExternalStationId }).IsUnique();

            builder.HasOne(m => m.Stop).WithMany(s => s.AgencyMappings).HasForeignKey(m => m.StopId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.Agency).WithMany().HasForeignKey(m => m.AgencyId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
