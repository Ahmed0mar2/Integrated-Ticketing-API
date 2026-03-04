using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GP.Domain.Entities;

namespace GP.Infrastructure.Data.Configurations;

public class TripStopTimeConfiguration : IEntityTypeConfiguration<TripStopTime>
{
    public void Configure(EntityTypeBuilder<TripStopTime> builder)
    {
        builder.HasKey(t => t.TripStopTimeId);

        builder.Property(t => t.TripId).IsRequired();
        builder.Property(t => t.StationId).IsRequired();
        builder.Property(t => t.StopSequence).IsRequired();
        builder.Property(t => t.ArrivalOffsetMinutes).IsRequired();
        builder.Property(t => t.DepartureOffsetMinutes).IsRequired();
        builder.Property(t => t.DistanceFromOriginKm).IsRequired().HasPrecision(10, 2);

        builder.HasIndex(t => new { t.TripId, t.StopSequence }).IsUnique();
        builder.HasIndex(t => new { t.TripId, t.StationId });

        builder.HasOne(t => t.Trip).WithMany(tr => tr.TripStopTimes).HasForeignKey(t => t.TripId);
        builder.HasOne(t => t.Station).WithMany(s => s.TripStopTimes).HasForeignKey(t => t.StationId);
    }
}