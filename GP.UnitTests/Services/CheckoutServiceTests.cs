using System;
using System.Collections.Generic;
using System.Linq;
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

public class CheckoutServiceTests
{
    [Fact]
    public async Task CheckoutAsync_Throws_InsufficientFunds_When_Cart_Exceeds_Balance()
    {
        using var connection = CreateConnection();
        var contextMock = CreateDbContext(connection);
        var now = AppTime.GetScheduleNow();

        SeedInsufficientWalletScenario(contextMock.Object, now);

        var service = CreateService(contextMock.Object);
        var request = new CheckoutRequestDto
        {
            PaymentMethod = "Wallet",
            PointsToRedeem = 0
        };

        Func<Task> act = () => service.CheckoutAsync(1, request);

        await act.Should().ThrowAsync<CartValidationException>()
            .WithMessage("*Insufficient funds*");

        var booking = contextMock.Object.Bookings.Single(b => b.BookingId == 1);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    private static BookingService CreateService(ApplicationDbContext context)
    {
        var loyaltyService = new Mock<ILoyaltyService>();
        var mediator = new Mock<IMediator>();
        var notificationService = new Mock<INotificationService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoyaltySettings:PointToEgpValue"] = "0.05",
                ["LoyaltySettings:MaxDiscountPercentage"] = "0.50",
                ["LoyaltySettings:EarnRatePercentage"] = "0.05"
            })
            .Build();

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

    private static void SeedInsufficientWalletScenario(ApplicationDbContext context, DateTime now)
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
            FirstName = "Wallet",
            LastName = "User",
            FamilyName = "Seed",
            Email = "wallet@example.com",
            Phone = "01000000001",
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Nationality = "Egyptian",
            CountryId = 1,
            Country = country,
            WalletBalance = 50.00m
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
            TotalPrice = 200m,
            BookingTime = now.AddMinutes(-2),
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            HoldExpiresAt = now.AddMinutes(8),
            ContactName = "Wallet",
            ContactPhone = "01000000001",
            ContactEmail = "wallet@example.com",
            BookingPassengers = new List<BookingPassenger>
            {
                new()
                {
                    PassengerId = 1,
                    Name = "Passenger One",
                    OccurrenceId = 1,
                    CoachClassId = 1,
                    SeatNumber = "5"
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
        context.Bookings.Add(booking);

        context.SaveChanges();
    }
}
