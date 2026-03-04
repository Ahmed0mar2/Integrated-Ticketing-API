using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using GP.Domain.Entities;

public static class CountrySeeder
{
    public static async Task SeedCountriesAsync(ApplicationDbContext context)
    {
        if (await context.Countries.AnyAsync())
        {
            Console.WriteLine("ℹ️  Countries already seeded");
            return;
        }

        var countries = new[]
        {
            // Middle East & North Africa (MENA)
            new Country { CountryCode = "EG", CountryName = "Egypt", NationalityName = "Egyptian", PhoneCode = "+20", AllowsTrainBooking = true },
            new Country { CountryCode = "SA", CountryName = "Saudi Arabia", NationalityName = "Saudi", PhoneCode = "+966", AllowsTrainBooking = false },
            new Country { CountryCode = "AE", CountryName = "United Arab Emirates", NationalityName = "Emirati", PhoneCode = "+971", AllowsTrainBooking = false },
            new Country { CountryCode = "KW", CountryName = "Kuwait", NationalityName = "Kuwaiti", PhoneCode = "+965", AllowsTrainBooking = false },
            new Country { CountryCode = "QA", CountryName = "Qatar", NationalityName = "Qatari", PhoneCode = "+974", AllowsTrainBooking = false },
            new Country { CountryCode = "BH", CountryName = "Bahrain", NationalityName = "Bahraini", PhoneCode = "+973", AllowsTrainBooking = false },
            new Country { CountryCode = "OM", CountryName = "Oman", NationalityName = "Omani", PhoneCode = "+968", AllowsTrainBooking = false },
            new Country { CountryCode = "JO", CountryName = "Jordan", NationalityName = "Jordanian", PhoneCode = "+962", AllowsTrainBooking = false },
            new Country { CountryCode = "LB", CountryName = "Lebanon", NationalityName = "Lebanese", PhoneCode = "+961", AllowsTrainBooking = false },
            new Country { CountryCode = "SY", CountryName = "Syria", NationalityName = "Syrian", PhoneCode = "+963", AllowsTrainBooking = false },
            new Country { CountryCode = "IQ", CountryName = "Iraq", NationalityName = "Iraqi", PhoneCode = "+964", AllowsTrainBooking = false },
            new Country { CountryCode = "YE", CountryName = "Yemen", NationalityName = "Yemeni", PhoneCode = "+967", AllowsTrainBooking = false },
            new Country { CountryCode = "PS", CountryName = "Palestine", NationalityName = "Palestinian", PhoneCode = "+970", AllowsTrainBooking = false },
            new Country { CountryCode = "LY", CountryName = "Libya", NationalityName = "Libyan", PhoneCode = "+218", AllowsTrainBooking = false },
            new Country { CountryCode = "SD", CountryName = "Sudan", NationalityName = "Sudanese", PhoneCode = "+249", AllowsTrainBooking = false },
            new Country { CountryCode = "MA", CountryName = "Morocco", NationalityName = "Moroccan", PhoneCode = "+212", AllowsTrainBooking = false },
            new Country { CountryCode = "DZ", CountryName = "Algeria", NationalityName = "Algerian", PhoneCode = "+213", AllowsTrainBooking = false },
            new Country { CountryCode = "TN", CountryName = "Tunisia", NationalityName = "Tunisian", PhoneCode = "+216", AllowsTrainBooking = false },
            
            // Common International
            new Country { CountryCode = "US", CountryName = "United States", NationalityName = "American", PhoneCode = "+1", AllowsTrainBooking = false },
            new Country { CountryCode = "GB", CountryName = "United Kingdom", NationalityName = "British", PhoneCode = "+44", AllowsTrainBooking = false },
            new Country { CountryCode = "FR", CountryName = "France", NationalityName = "French", PhoneCode = "+33", AllowsTrainBooking = false },
            new Country { CountryCode = "DE", CountryName = "Germany", NationalityName = "German", PhoneCode = "+49", AllowsTrainBooking = false },
            new Country { CountryCode = "IT", CountryName = "Italy", NationalityName = "Italian", PhoneCode = "+39", AllowsTrainBooking = false },
            new Country { CountryCode = "ES", CountryName = "Spain", NationalityName = "Spanish", PhoneCode = "+34", AllowsTrainBooking = false },
            new Country { CountryCode = "TR", CountryName = "Turkey", NationalityName = "Turkish", PhoneCode = "+90", AllowsTrainBooking = false },
            new Country { CountryCode = "CN", CountryName = "China", NationalityName = "Chinese", PhoneCode = "+86", AllowsTrainBooking = false },
            new Country { CountryCode = "IN", CountryName = "India", NationalityName = "Indian", PhoneCode = "+91", AllowsTrainBooking = false },
            new Country { CountryCode = "JP", CountryName = "Japan", NationalityName = "Japanese", PhoneCode = "+81", AllowsTrainBooking = false },
            new Country { CountryCode = "KR", CountryName = "South Korea", NationalityName = "Korean", PhoneCode = "+82", AllowsTrainBooking = false },
            new Country { CountryCode = "BR", CountryName = "Brazil", NationalityName = "Brazilian", PhoneCode = "+55", AllowsTrainBooking = false },
            new Country { CountryCode = "CA", CountryName = "Canada", NationalityName = "Canadian", PhoneCode = "+1", AllowsTrainBooking = false },
            new Country { CountryCode = "AU", CountryName = "Australia", NationalityName = "Australian", PhoneCode = "+61", AllowsTrainBooking = false },
            new Country { CountryCode = "RU", CountryName = "Russia", NationalityName = "Russian", PhoneCode = "+7", AllowsTrainBooking = false },
            
            //TODO:  Add more as needed...
        };

        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Seeded {countries.Length} countries");
    }
}
