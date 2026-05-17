using GP.Application.DTOs.Bookings;
using GP.Application.Interfaces;
using GP.Application.Settings;
using GP.Domain.Common;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace GP.Application.Services
{
    public class BoardingPassService : IBoardingPassService
    {
        private const string PayloadPrefix = "GP";
        private readonly ApplicationDbContext _dbContext;
        private readonly BoardingPassSettings _settings;

        public BoardingPassService(
            ApplicationDbContext dbContext,
            IOptions<BoardingPassSettings> settings)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
        }

        public async Task<string> GenerateBoardingPassPayloadAsync(
            int userId,
            int bookingId,
            int passengerId,
            CancellationToken cancellationToken = default)
        {
            var booking = await _dbContext.Bookings
                .AsNoTracking()
                .Include(b => b.BookingPassengers)
                .Include(b => b.Occurrence)
                    .ThenInclude(o => o.Trip)
                        .ThenInclude(t => t.TripStopTimes)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken);

            if (booking == null || booking.UserId != userId || booking.Status != BookingStatus.Confirmed)
                throw new BadHttpRequestException("Invalid booking or passenger.");

            var passenger = booking.BookingPassengers.FirstOrDefault(p => p.PassengerId == passengerId);
            if (passenger == null)
                throw new BadHttpRequestException("Invalid booking or passenger.");

            var boardingTime = ResolvePassengerLocalTimes(booking).BoardingTime;
            var expUnix = ToUnixSeconds(boardingTime.AddHours(2));

            var rawData = $"{PayloadPrefix}|{bookingId}|{passengerId}|{userId}|{expUnix}";
            var signature = ComputeSignature(rawData);

            return $"{rawData}|{signature}";
        }

        public async Task<VerifyPassResponseDto> VerifyBoardingPassAsync(
            string payload,
            CancellationToken cancellationToken = default)
        {
            var parsed = ParsePayload(payload);
            ValidateSignature(parsed.RawData, parsed.Signature);

            if (parsed.ExpUnix < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                throw new BadHttpRequestException("Boarding pass has expired.");

            var booking = await _dbContext.Bookings
                .AsNoTracking()
                .Include(b => b.BookingPassengers)
                .FirstOrDefaultAsync(b => b.BookingId == parsed.BookingId, cancellationToken);

            if (booking == null)
                throw new BadHttpRequestException("Booking not found.");

            if (booking.UserId != parsed.UserId)
                throw new BadHttpRequestException("Ticket Ownership Invalid - This ticket has been transferred.");

            var passenger = booking.BookingPassengers.FirstOrDefault(p => p.PassengerId == parsed.PassengerId);
            if (passenger == null)
                throw new BadHttpRequestException("Passenger not found.");

            return new VerifyPassResponseDto
            {
                PassengerName = passenger.Name,
                SeatNumber = passenger.SeatNumber
            };
        }

        private sealed record ParsedPayload(
            int BookingId,
            int PassengerId,
            int UserId,
            long ExpUnix,
            string RawData,
            string Signature);

        private static ParsedPayload ParsePayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                throw new BadHttpRequestException("Boarding pass payload is required.");

            var parts = payload.Trim().Split('|');

            if (parts.Length != 6 || !string.Equals(parts[0], PayloadPrefix, StringComparison.Ordinal))
                throw new BadHttpRequestException("Invalid boarding pass payload format.");

            if (!int.TryParse(parts[1], out var bookingId)
                || !int.TryParse(parts[2], out var passengerId)
                || !int.TryParse(parts[3], out var userId)
                || !long.TryParse(parts[4], out var expUnix))
            {
                throw new BadHttpRequestException("Invalid boarding pass payload format.");
            }

            var signature = parts[5];
            if (string.IsNullOrWhiteSpace(signature))
                throw new BadHttpRequestException("Invalid boarding pass payload format.");

            var rawData = string.Join("|", parts.Take(5));

            return new ParsedPayload(bookingId, passengerId, userId, expUnix, rawData, signature);
        }

        private void ValidateSignature(string rawData, string signature)
        {
            var expected = ComputeSignatureBytes(rawData);
            byte[] provided;

            try
            {
                provided = Convert.FromBase64String(signature);
            }
            catch (FormatException)
            {
                throw new UnauthorizedAccessException("Invalid boarding pass signature.");
            }

            if (expected.Length != provided.Length
                || !CryptographicOperations.FixedTimeEquals(expected, provided))
            {
                throw new UnauthorizedAccessException("Invalid boarding pass signature.");
            }
        }

        private string ComputeSignature(string rawData)
        {
            var bytes = ComputeSignatureBytes(rawData);
            return Convert.ToBase64String(bytes);
        }

        private byte[] ComputeSignatureBytes(string rawData)
        {
            if (string.IsNullOrWhiteSpace(_settings.QrSecretKey))
                throw new InvalidOperationException("BoardingPassSettings.QrSecretKey is not configured.");

            var keyBytes = Encoding.UTF8.GetBytes(_settings.QrSecretKey);
            var dataBytes = Encoding.UTF8.GetBytes(rawData);

            using var hmac = new HMACSHA256(keyBytes);
            return hmac.ComputeHash(dataBytes);
        }

        private static long ToUnixSeconds(DateTime localTime)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(localTime);
            var dateTimeOffset = new DateTimeOffset(localTime, offset);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        private static (DateTime BoardingTime, DateTime DropoffTime) ResolvePassengerLocalTimes(Booking booking)
        {
            var fallbackBoarding = AppTime.AsSchedule(booking.Occurrence.DepartureDateTime);
            var fallbackDropoff = AppTime.AsSchedule(booking.Occurrence.ArrivalDateTime);

            var trip = booking.Occurrence.Trip;
            if (trip?.TripStopTimes == null || trip.TripStopTimes.Count == 0)
                return (fallbackBoarding, fallbackDropoff);

            var fromStop = trip.TripStopTimes
                .Where(ts => ts.StationId == booking.OriginStationId)
                .OrderBy(ts => ts.StopSequence)
                .FirstOrDefault();

            var toStop = trip.TripStopTimes
                .Where(ts => ts.StationId == booking.DestinationStationId)
                .OrderBy(ts => ts.StopSequence)
                .FirstOrDefault();

            if (fromStop == null || toStop == null || fromStop.StopSequence >= toStop.StopSequence)
                return (fallbackBoarding, fallbackDropoff);

            var boardingTimeOnly = fromStop.DepartureTime ?? fromStop.ArrivalTime;
            var dropoffTimeOnly = toStop.ArrivalTime ?? toStop.DepartureTime;

            if (!boardingTimeOnly.HasValue || !dropoffTimeOnly.HasValue)
                return (fallbackBoarding, fallbackDropoff);

            var boardingTime = BuildSegmentDateTime(
                booking.Occurrence.DepartureDateTime,
                trip.DepartureTime,
                boardingTimeOnly.Value);

            var dropoffTime = BuildSegmentDateTime(
                booking.Occurrence.DepartureDateTime,
                trip.DepartureTime,
                dropoffTimeOnly.Value);

            return (
                AppTime.AsSchedule(boardingTime),
                AppTime.AsSchedule(dropoffTime));
        }

        private static DateTime BuildSegmentDateTime(
            DateTime occurrenceStart,
            TimeOnly tripOriginDeparture,
            TimeOnly segmentTime)
        {
            var offset = segmentTime.ToTimeSpan() - tripOriginDeparture.ToTimeSpan();
            if (offset < TimeSpan.Zero)
                offset = offset.Add(TimeSpan.FromDays(1));

            return occurrenceStart.Add(offset);
        }
    }
}
