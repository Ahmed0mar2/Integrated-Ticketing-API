using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            await context.Database.MigrateAsync();

            //Seed countries
            await CountrySeeder.SeedCountriesAsync(context);

            //seed roles
            await SeedRolesAsync(roleManager);

            //seed admin user
            await SeedAdminUserAsync(userManager, context);
        }
        private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roles = { "Admin", "User", "Partner" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                    Console.WriteLine($"Role '{role}' created");
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            const string adminEmail = "admin@gp.com";
            const string adminPassword = "Admin@123456";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                Console.WriteLine("ℹ️  Admin user already exists");
                return;
            }

            var egyptCountry = await context.Countries.FirstOrDefaultAsync(c => c.CountryCode == "EG");
            if (egyptCountry == null)
            {
                Console.WriteLine("❌ Egypt country not found. Run country seeder first.");
                return;
            }

            var applicationUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                PhoneNumber = "+201000000000",
                IsActive = true
            };

            var result = await userManager.CreateAsync(applicationUser, adminPassword);

            if (!result.Succeeded)
            {
                Console.WriteLine($"❌ Failed to create admin user");
                return;
            }

            var domainUser = new User
            {
                Email = adminEmail,
                Phone = "+201000000000",
                FirstName = "System",
                LastName = "Admin",
                FamilyName = "Administrator",
                Gender = Gender.Male,
                DateOfBirth = new DateOnly(1990, 1, 1),
                NationalIdNumber = "00000000000000",
                IsNationalIdVerified = true,
                CountryId = egyptCountry.CountryId,
                Nationality = "Egyptian", 
                TotalTripsCount = 0,
                TotalDistanceTraveled = 0,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(domainUser);
            await context.SaveChangesAsync();

            applicationUser.DomainUserId = domainUser.UserId;
            await userManager.UpdateAsync(applicationUser);
            await userManager.AddToRoleAsync(applicationUser, "Admin");

            Console.WriteLine("✅ Admin user created successfully!");
        }

    }
}
