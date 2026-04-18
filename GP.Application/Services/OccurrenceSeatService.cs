using GP.Application.DTOs.Occurrences;
using GP.Application.Interfaces;
using GP.Application.Common;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GP.Application.Services;

public class OccurrenceSeatService : IOccurrenceSeatService
{
    private readonly ApplicationDbContext _dbContext;

    public OccurrenceSeatService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OccurrenceSeatsResponseDto?> GetOccurrenceSeatsAsync(int occurrenceId, CancellationToken cancellationToken = default)
    {
        var occurrence = await _dbContext.TripOccurrences
            .AsNoTracking()
            .Include(o => o.ClassInventories)
                .ThenInclude(ci => ci.CoachClass)
            .FirstOrDefaultAsync(o => o.TripOccurrenceId == occurrenceId, cancellationToken);

        if (occurrence == null)
            return null;

        var now = DateTime.UtcNow;

        var seatLocks = await _dbContext.BookingPassengers
            .AsNoTracking()
            .Where(p => p.OccurrenceId == occurrenceId)
            .Select(p => new
            {
                p.CoachClassId,
                p.SeatNumber,
                p.BookingId,
                BookingStatus = p.Booking.Status,
                p.Booking.HoldExpiresAt
            })
            .ToListAsync(cancellationToken);

        var response = new OccurrenceSeatsResponseDto
        {
            OccurrenceId = occurrenceId,
            GeneratedAtUtc = now,
            Classes = []
        };

        foreach (var inventory in occurrence.ClassInventories.OrderBy(ci => ci.CoachClassId))
        {
            var allSeats = Enumerable.Range(1, inventory.TotalSeats)
                .Select(n => n.ToString())
                .ToList();

            var stateBySeat = new Dictionary<string, OccurrenceSeatDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var seatLock in seatLocks.Where(s => s.CoachClassId == inventory.CoachClassId))
            {
                var seatNumber = (seatLock.SeatNumber ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(seatNumber))
                    continue;

                var status = ResolveSeatStatus(seatLock.BookingStatus, seatLock.HoldExpiresAt, now);
                if (status == null)
                    continue;

                if (!allSeats.Contains(seatNumber, StringComparer.OrdinalIgnoreCase))
                    allSeats.Add(seatNumber);

                if (stateBySeat.TryGetValue(seatNumber, out var existing))
                {
                    if (existing.Status == "Booked")
                        continue;

                    if (existing.Status == "Pending" && status == "Pending")
                        continue;
                }

                stateBySeat[seatNumber] = new OccurrenceSeatDto
                {
                    SeatNumber = seatNumber,
                    Status = status,
                    BookingId = seatLock.BookingId,
                    HoldExpiresAt = status == "Pending" && seatLock.HoldExpiresAt.HasValue
                        ? AppTime.AsUtc(seatLock.HoldExpiresAt.Value)
                        : null
                };
            }

            var orderedSeats = allSeats
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(seat => int.TryParse(seat, out _) ? 0 : 1)
                .ThenBy(seat => int.TryParse(seat, out var n) ? n : int.MaxValue)
                .ThenBy(seat => seat, StringComparer.OrdinalIgnoreCase)
                .Select(seat => stateBySeat.TryGetValue(seat, out var dto)
                    ? dto
                    : new OccurrenceSeatDto
                    {
                        SeatNumber = seat,
                        Status = "Available"
                    })
                .ToList();

            response.Classes.Add(new OccurrenceClassSeatsDto
            {
                CoachClassId = inventory.CoachClassId,
                ClassName = inventory.CoachClass.Name,
                TotalSeats = inventory.TotalSeats,
                RemainingSeats = inventory.RemainingSeats,
                LayoutType = inventory.CoachClass.LayoutType,
                DeckCount = inventory.CoachClass.DeckCount,
                SeatMapJson = inventory.CoachClass.SeatMapJson,
                AvailableCount = orderedSeats.Count(s => s.Status == "Available"),
                PendingCount = orderedSeats.Count(s => s.Status == "Pending"),
                BookedCount = orderedSeats.Count(s => s.Status == "Booked"),
                Seats = orderedSeats
            });
        }

        return response;
    }

    private static string? ResolveSeatStatus(BookingStatus bookingStatus, DateTime? holdExpiresAt, DateTime now)
    {
        if (bookingStatus is BookingStatus.Confirmed or BookingStatus.Completed)
            return "Booked";

        if (bookingStatus == BookingStatus.Pending && holdExpiresAt.HasValue && holdExpiresAt.Value > now)
            return "Pending";

        return null;
    }
}
