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
        builder.Property(t => t.ArrivalTime).IsRequired(false).HasColumnType("time");
        builder.Property(t => t.DepartureTime).IsRequired(false).HasColumnType("time");

        builder.HasIndex(t => new { t.TripId, t.StopSequence }).IsUnique();

        builder.HasOne(t => t.Trip).WithMany(tr => tr.TripStopTimes).HasForeignKey(t => t.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Station).WithMany(s => s.TripStopTimes).HasForeignKey(t => t.StationId).OnDelete(DeleteBehavior.Restrict);
    }
}