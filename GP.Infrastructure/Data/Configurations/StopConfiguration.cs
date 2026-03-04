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
    public class StopConfiguration : IEntityTypeConfiguration<Stop>
    {
        public void Configure(EntityTypeBuilder<Stop> builder)
        {
            builder.HasKey(s => s.StopId);
            builder.Property(s => s.StopName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.City).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Latitude).IsRequired().HasPrecision(10, 6);
            builder.Property(s => s.Longitude).IsRequired().HasPrecision(10, 6);

            builder.HasIndex(s => new { s.StopName, s.City }).IsUnique();
            builder.HasIndex(s => s.City);

            builder.HasMany(s => s.TripStopTimes).WithOne(tst => tst.Station).HasForeignKey(tst => tst.StationId);
        }
    }
}
