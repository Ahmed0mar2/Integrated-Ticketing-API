using GP.Domain.Common;
using GP.Domain.Entities;
using GP.Infrastructure.Data.Configurations;
using GP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Domain DbSets
        public DbSet<Agency> Agencies { get; set; }
        public DbSet<Stop> Stops { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Calendar> Calendars { get; set; }
        public DbSet<CalendarDate> CalendarDates { get; set; }
        public DbSet<TripOccurrence> TripOccurrences { get; set; }
        public new DbSet<User> Users { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingPassenger> BookingPassengers { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<TripStopTime> TripStopTimes { get; set; }
        public DbSet<TripOccurrenceClassInventory> TripOccurrenceClassInventories { get; set; }
        public DbSet<TripClassPricing> TripClassPricings { get; set; }
        public DbSet<TrainType> TrainTypes { get; set; }
        public DbSet<CoachClass> CoachClasses { get; set; }
        public DbSet<TrainTypeCoachConfig> TrainTypeCoachConfigs { get; set; }
        public DbSet<MarketplaceListing> MarketplaceListings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Identity tables 
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims");

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");

                    var isDeletedProperty =
                        Expression.Property(parameter, nameof(BaseEntity.IsDeleted));

                    var filter = Expression.Lambda(
                        Expression.Equal(isDeletedProperty, Expression.Constant(false)),
                        parameter
                    );

                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(filter);
                }
            }
            // Apply domain configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }

}
