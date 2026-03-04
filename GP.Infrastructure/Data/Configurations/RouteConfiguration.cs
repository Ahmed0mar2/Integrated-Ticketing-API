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
    public class RouteConfiguration : IEntityTypeConfiguration<Route>
    {
        public void Configure(EntityTypeBuilder<Route> builder)
        {
            builder.ToTable("routes");

            builder.HasKey(r => r.RouteId);

            builder.Property(r => r.RouteId)
                .HasColumnName("route_id");

            builder.Property(r => r.AgencyId)
                .HasColumnName("agency_id");

            builder.Property(r => r.RouteName)
                .HasColumnName("route_name")
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(r => r.Agency)
                .WithMany(a => a.Routes)  
                .HasForeignKey(r => r.AgencyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.AgencyId);
        }
    }
}
