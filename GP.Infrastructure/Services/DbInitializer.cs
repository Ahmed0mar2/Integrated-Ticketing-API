using GP.Domain.Common;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using GP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GP.Infrastructure.Services
{
    public static class DbInitializer
    {
        private const string AdminRole = "Admin";
        private const string UserRole = "User";
        private const string PartnerRole = "Partner";

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            await context.Database.MigrateAsync();

            // Seed countries
            await CountrySeeder.SeedCountriesAsync(context);

            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed challenges
            await SeedChallengesAsync(context);

            // Seed admin user
            await SeedAdminUserAsync(userManager, context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roles = { AdminRole, UserRole, PartnerRole };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                    Console.WriteLine($"Role '{role}' created");
                }
            }
        }

        private static async Task SeedChallengesAsync(ApplicationDbContext context)
        {
            // Only skip if we already have OneTime challenges seeded
            if (await context.Challenges.AnyAsync(c => c.Frequency == ChallengeFrequency.OneTime))
            {
                Console.WriteLine("ℹ️  Welcome Quests already exist");
                return;
            }

            var welcomeQuests = new List<Challenge>
{
    new Challenge {
        Title = "Complete your first booking", TitleAr = "حجزك الأول",
        Description = "Complete your first booking to unlock your onboarding reward.", DescriptionAr = "أكمل حجزك الأول لتحصل على مكافأة الترحيب الخاصة بك.",
        Type = ChallengeType.TotalTrips, GoalValue = 1, RewardPoints = 400, IsActive = true, Frequency = ChallengeFrequency.OneTime
    },
    new Challenge {
        Title = "Complete 3 bookings", TitleAr = "إنجاز 3 حجوزات",
        Description = "Complete 3 bookings to build your momentum.", DescriptionAr = "أكمل 3 حجوزات لتزيد من رصيد نقاطك.",
        Type = ChallengeType.TotalTrips, GoalValue = 3, RewardPoints = 600, IsActive = true, Frequency = ChallengeFrequency.OneTime
    },
    new Challenge {
        Title = "Try a round trip", TitleAr = "رحلة متكاملة",
        Description = "Book and complete a round trip to earn this quest reward.", DescriptionAr = "احجز وأكمل رحلة ذهاب وعودة لتحصل على هذه المكافأة.",
        Type = ChallengeType.RoundTrip, GoalValue = 1, RewardPoints = 500, IsActive = true, Frequency = ChallengeFrequency.OneTime
    },
    new Challenge {
        Title = "Master the system", TitleAr = "خبير الرحلات",
        Description = "Book a multi-destination trip to prove you're a travel pro.", DescriptionAr = "احجز رحلة لعدة وجهات لتثبت خبرتك في التخطيط.",
        Type = ChallengeType.MultiDestination, GoalValue = 1, RewardPoints = 800, IsActive = true, Frequency = ChallengeFrequency.OneTime
    },
    new Challenge {
        Title = "Big Spender", TitleAr = "العميل الذهبي",
        Description = "Spend a total of 1,500 EGP to unlock this lifetime badge.", DescriptionAr = "أنفق إجمالي 1,500 جنيه لتفتح هذه الشارة الدائمة.",
        Type = ChallengeType.TotalSpend, GoalValue = 1500, RewardPoints = 800, IsActive = true, Frequency = ChallengeFrequency.OneTime
    }
};

            context.Challenges.AddRange(welcomeQuests);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ {welcomeQuests.Count} Welcome Quests (OneTime challenges) created successfully!");
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
                Console.WriteLine("❌ Failed to create admin user");
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
                WalletBalance = 0m,
                LoyaltyPointsBalance = 0,
                CreatedAt = AppTime.GetScheduleNow()
            };

            context.Users.Add(domainUser);
            await context.SaveChangesAsync();

            applicationUser.DomainUserId = domainUser.UserId;
            await userManager.UpdateAsync(applicationUser);
            await userManager.AddToRoleAsync(applicationUser, AdminRole);

            Console.WriteLine("✅ Admin user created successfully!");
        }
    }
}
