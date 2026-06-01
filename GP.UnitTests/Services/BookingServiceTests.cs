using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GP.Application.Common;
using GP.Application.DTOs.Bookings;
using GP.Application.Interfaces;
using GP.Application.Services;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GP.UnitTests.Services;

public class BookingServiceTests
{
    [Fact]
    public async Task AddToCartAsync_Fails_When_Seat_Is_Already_Locked()
    {
        using var connection = CreateConnection();
        var contextMock = CreateDbContext(connection);
        var now = AppTime.GetScheduleNow();

        SeedSeatLockScenario(contextMock.Object, now);

        var service = CreateService(contextMock.Object);
        var request = new AddToCartRequestDto
        {
            TripOccurrenceId = 1,
            CoachClassId = 1,
            OriginStationId = 1,
            DestinationStationId = 2,
            ContactName = "Test User",
            ContactPhone = "0123456789",
            ContactEmail = "user@example.com",
            Passengers = new List<PassengerDetailDto>
            {
                new()
                {
                    PassengerName = "Passenger One",
                    SeatNumber = "1"
                }
            }
        };

        Func<Task> act = () => service.AddToCartAsync(2, request);

        await act.Should().ThrowAsync<CartConcurrencyException>();
    }

    private static BookingService CreateService(ApplicationDbContext context)
    {
        var loyaltyService = new Mock<ILoyaltyService>();
        var mediator = new Mock<IMediator>();
        var notificationService = new Mock<INotificationService>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        return new BookingService(
            context,
            loyaltyService.Object,
            mediator.Object,
            configuration,
            notificationService.Object);
    }

    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static Mock<ApplicationDbContext> CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var contextMock = new Mock<ApplicationDbContext>(options) { CallBase = true };
        contextMock.Object.Database.EnsureCreated();

        return contextMock;
    }

    private static void SeedSeatLockScenario(ApplicationDbContext context, DateTime now)
    {
        var country = new Country
        {
            CountryId = 1,
            CountryCode = "EG",
            CountryName = "Egypt",
            NationalityName = "Egyptian",
            AllowsTrainBooking = true
        };

        var user = new User
        {
            UserId = 1,
            FirstName = "Seed",
            LastName = "User",
            FamilyName = "Seed",
            Email = "seed@example.com",
            Phone = "01000000000",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Nationality = "Egyptian",
            CountryId = 1,
            Country = country
        };

        var agency = new Agency
        {
            AgencyId = 1,
            AgencyName = "Test Agency",
            AgencyType = AgencyType.Bus
        };

        var calendar = new Calendar
        {
            ServiceId = 1,
            Monday = true,
            Tuesday = true,
            Wednesday = true,
            Thursday = true,
            Friday = true,
            Saturday = true,
            Sunday = true,
            StartDate = DateOnly.FromDateTime(now.Date),
            EndDate = DateOnly.FromDateTime(now.Date.AddDays(30))
        };

        var origin = new Stop
        {
            StopId = 1,
            ArabicName = "Origin",
            NormalizedSlug = "origin",
            City = "Cairo",
            Governorate = "Cairo"
        };

        var destination = new Stop
        {
            StopId = 2,
            ArabicName = "Destination",
            NormalizedSlug = "destination",
            City = "Giza",
            Governorate = "Giza"
        };

        var coachClass = new CoachClass
        {
            CoachClassId = 1,
            Name = "Economy",
            DefaultCapacity = 40
        };

        var trip = new Trip
        {
            TripId = 1,
            AgencyId = 1,
            Agency = agency,
            OriginStationId = 1,
            DestinationStationId = 2,
            ServiceId = 1,
            Calendar = calendar,
            DepartureTime = new TimeOnly(8, 0)
        };

        var occurrence = new TripOccurrence
        {
            TripOccurrenceId = 1,
            TripId = 1,
            Trip = trip,
            OccurrenceDate = DateOnly.FromDateTime(now.Date),
            DepartureDateTime = now.AddHours(2),
            ArrivalDateTime = now.AddHours(5),
            IsActive = true
        };

        var inventory = new TripOccurrenceClassInventory
        {
            TripOccurrenceClassInventoryId = 1,
            TripOccurrenceId = 1,
            TripOccurrence = occurrence,
            CoachClassId = 1,
            CoachClass = coachClass,
            TotalSeats = 40,
            RemainingSeats = 39,
            RowVersion = new byte[] { 1 }
        };

        var fare = new TripFare
        {
            TripFareId = 1,
            TripId = 1,
            Trip = trip,
            OriginStationId = 1,
            DestinationStationId = 2,
            CoachClassId = 1,
            CoachClass = coachClass,
            Price = 100m
        };

        var booking = new Booking
        {
            BookingId = 1,
            UserId = 1,
            User = user,
            OccurrenceId = 1,
            Occurrence = occurrence,
            CoachClassId = 1,
            CoachClass = coachClass,
            OriginStationId = 1,
            OriginStation = origin,
            DestinationStationId = 2,
            DestinationStation = destination,
            SeatsBooked = 1,
            TotalPrice = 100m,
            BookingTime = now.AddMinutes(-2),
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            HoldExpiresAt = now.AddMinutes(8),
            ContactName = "Seed",
            ContactPhone = "01000000000",
            ContactEmail = "seed@example.com",
            BookingPassengers = new List<BookingPassenger>
            {
                new()
                {
                    PassengerId = 1,
                    Name = "Passenger One",
                    OccurrenceId = 1,
                    CoachClassId = 1,
                    SeatNumber = "1"
                }
            }
        };

        context.Countries.Add(country);
        context.Users.Add(user);
        context.Agencies.Add(agency);
        context.Calendars.Add(calendar);
        context.Stops.AddRange(origin, destination);
        context.CoachClasses.Add(coachClass);
        context.Trips.Add(trip);
        context.TripOccurrences.Add(occurrence);
        context.SaveChanges();
        context.Database.ExecuteSqlRaw(@"INSERT INTO TripOccurrenceClassInventories
            (TripOccurrenceClassInventoryId, TripOccurrenceId, CoachClassId, TotalSeats, RemainingSeats, RowVersion)
            VALUES (1, 1, 1, 40, 39, x'01')");
        context.TripFares.Add(fare);
        context.Bookings.Add(booking);

        context.SaveChanges();
    }
}
