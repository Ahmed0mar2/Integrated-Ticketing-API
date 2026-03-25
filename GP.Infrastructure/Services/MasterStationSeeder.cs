using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using GP.Infrastructure.Data.SeedData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GP.Infrastructure.Services
{
    public class MasterStationSeeder
    {
        private readonly ApplicationDbContext _context;

        public MasterStationSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedStationsAsync(string jsonFilePath)
        {
            Console.WriteLine("Verifying Agencies...");

            var trainAgency = await GetOrCreateAgencyAsync("Egyptian National Railways", AgencyType.Train); 
            var gobusAgency = await GetOrCreateAgencyAsync("GoBus", AgencyType.Bus);
            var horusAgency = await GetOrCreateAgencyAsync("Horus", AgencyType.Bus);
            var bluebusAgency = await GetOrCreateAgencyAsync("Blue Bus", AgencyType.Bus);

            // 2. Read the JSON File
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"Error: Cannot find file at {jsonFilePath}");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(jsonFilePath);
            var stations = JsonSerializer.Deserialize<List<MasterStationDto>>(jsonString);

            if (stations == null || !stations.Any())
            {
                Console.WriteLine("No stations found in the JSON file.");
                return;
            }

            Console.WriteLine($"Found {stations.Count} master stations. Beginning import...");

            // 3. Process each station
            int addedCount = 0;
            foreach (var dto in stations)
            {
                bool exists = await _context.Stops.AnyAsync(s => s.NormalizedSlug == dto.NormalizedSlug);
                if (exists) continue;

                var newStop = new Stop
                {
                    ArabicName = dto.Arabic,
                    NormalizedSlug = dto.NormalizedSlug,
                    City = dto.City,
                    Governorate = dto.Governorate,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude
                };

                // Use the dynamically fetched IDs!
                if (!string.IsNullOrEmpty(dto.Mappings.TrainSlug))
                {
                    newStop.AgencyMappings.Add(new StopAgencyMapping
                    {
                        AgencyId = trainAgency.AgencyId,
                        ExternalStationId = dto.Mappings.TrainSlug
                    });
                }

                if (dto.Mappings.GoBusId != null)
                {
                    newStop.AgencyMappings.Add(new StopAgencyMapping
                    {
                        AgencyId = gobusAgency.AgencyId,
                        ExternalStationId = dto.Mappings.GoBusId.ToString()!
                    });
                }

                if (dto.Mappings.HorusId != null)
                {
                    newStop.AgencyMappings.Add(new StopAgencyMapping
                    {
                        AgencyId = horusAgency.AgencyId,
                        ExternalStationId = dto.Mappings.HorusId.ToString()!
                    });
                }

                if (!string.IsNullOrEmpty(dto.Mappings.BlueBusSlug))
                {
                    newStop.AgencyMappings.Add(new StopAgencyMapping
                    {
                        AgencyId = bluebusAgency.AgencyId,
                        ExternalStationId = dto.Mappings.BlueBusSlug
                    });
                }

                _context.Stops.Add(newStop);
                addedCount++;
            }

            // 4. Save everything
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Successfully imported {addedCount} new Master Stations to the database!");
        }

        // Helper Method to Safely Get or Create Agencies
        private async Task<Agency> GetOrCreateAgencyAsync(string name, AgencyType type)
        {
            var agency = await _context.Agencies.FirstOrDefaultAsync(a => a.AgencyName == name);
            if (agency == null)
            {
                agency = new Agency { AgencyName = name, AgencyType = type };
                _context.Agencies.Add(agency);
                await _context.SaveChangesAsync(); // Save immediately to generate the ID
            }
            return agency;
        }
    }
}
