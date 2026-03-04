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
    public class CalendarDateConfiguration : IEntityTypeConfiguration<CalendarDate>
    {
        public void Configure(EntityTypeBuilder<CalendarDate> builder)
        {
            builder.HasKey(cd => cd.CalendarDateId);

            builder.Property(cd => cd.ServiceId).IsRequired();
            builder.Property(cd => cd.Date).IsRequired();
            builder.Property(cd => cd.ExceptionType).IsRequired().HasMaxLength(50);

            builder.HasIndex(cd => new { cd.ServiceId, cd.Date }).IsUnique();

            builder.HasOne(cd => cd.Calendar)
                .WithMany(c => c.CalendarDates)
                .HasForeignKey(cd => cd.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
