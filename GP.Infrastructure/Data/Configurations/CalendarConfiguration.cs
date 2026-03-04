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
    public class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
    {
        public void Configure(EntityTypeBuilder<Calendar> builder)
        {
            builder.HasKey(c => c.ServiceId);

            builder.Property(c => c.StartDate).IsRequired();
            builder.Property(c => c.EndDate).IsRequired();

            // All day-of-week bools required
            builder.Property(c => c.Monday).IsRequired();
            builder.Property(c => c.Tuesday).IsRequired();
            builder.Property(c => c.Wednesday).IsRequired();
            builder.Property(c => c.Thursday).IsRequired();
            builder.Property(c => c.Friday).IsRequired();
            builder.Property(c => c.Saturday).IsRequired();
            builder.Property(c => c.Sunday).IsRequired();

            builder.HasMany(c => c.CalendarDates)
                .WithOne(cd => cd.Calendar)
                .HasForeignKey(cd => cd.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
