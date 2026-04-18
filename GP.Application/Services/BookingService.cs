using GP.Application.Common;
using GP.Application.DTOs.Bookings;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GP.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _dbContext;

        public BookingService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<BookingCartResponseDto> AddToCartAsync(int userId, AddToCartRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new CartValidationException("Invalid request data. Please check your JSON format.");

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (request.Passengers == null || request.Passengers.Count == 0)
                        throw new CartValidationException("You must provide at least one passenger.");

                    var requestedSeats = request.Passengers.Count;
                    var requestedSeatNumbers = GetRequestedSeatNumbers(request);
                    var requestedPassengerIds = GetRequestedPassengerIds(request);

                    if (requestedSeatNumbers.Any(string.IsNullOrWhiteSpace))
                        throw new CartValidationException("Seat number is required for each passenger.");

                    if (requestedPassengerIds.Any(string.IsNullOrWhiteSpace))
                        throw new CartValidationException("Passenger ID/Passport number is required for each passenger.");

                    var normalizedRequestedPassengerIds = NormalizePassengerIds(requestedPassengerIds);

                    if (requestedSeatNumbers.Count != requestedSeatNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                        throw new CartValidationException("Seat numbers must be unique within the same cart item.");

                    var inventory = await _dbContext.TripOccurrenceClassInventories
                        .FirstOrDefaultAsync(i => i.TripOccurrenceId == request.TripOccurrenceId
                                               && i.CoachClassId == request.CoachClassId, cancellationToken);

                    if (inventory == null)
                        throw new CartValidationException("Class inventory not found for this trip.");

                    if (inventory.RemainingSeats < requestedSeats)
                        throw new CartValidationException($"Only {inventory.RemainingSeats} seats remaining. Cannot book {requestedSeats} seats.");

                    var outOfRangeSeats = requestedSeatNumbers
                        .Where(seat => !int.TryParse(seat, out var seatNo) || seatNo <= 0 || seatNo > inventory.TotalSeats)
                        .ToList();

                    if (outOfRangeSeats.Count > 0)
                        throw new CartValidationException("One or more selected seats are invalid for this class.");

                    var now = DateTime.UtcNow;
                    var lockedSeatNumbers = await _dbContext.BookingPassengers
                        .Where(p => p.OccurrenceId == request.TripOccurrenceId
                                 && p.CoachClassId == request.CoachClassId)
                        .Where(p => p.Booking.Status == BookingStatus.Confirmed
                                 || (p.Booking.Status == BookingStatus.Pending
                                     && p.Booking.HoldExpiresAt.HasValue
                                     && p.Booking.HoldExpiresAt.Value > now))
                        .Select(p => p.SeatNumber)
                        .ToListAsync(cancellationToken);

                    var conflictingSeats = requestedSeatNumbers
                        .Intersect(lockedSeatNumbers, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (conflictingSeats.Count > 0)
                        throw new CartConcurrencyException("One or more selected seats were just taken. Please refresh the seat map.");

                    var tripId = await _dbContext.TripOccurrences
                        .AsNoTracking()
                        .Where(o => o.TripOccurrenceId == request.TripOccurrenceId)
                        .Select(o => (int?)o.TripId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (!tripId.HasValue)
                        throw new CartValidationException("Trip occurrence not found.");

                    var fare = await _dbContext.TripFares
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f => f.TripId == tripId.Value
                                               && f.OriginStationId == request.OriginStationId
                                               && f.DestinationStationId == request.DestinationStationId
                                               && f.CoachClassId == request.CoachClassId, cancellationToken);

                    if (fare == null)
                        throw new CartValidationException("Pricing not found for this route and class.");

                    var duplicatePassengerId = await _dbContext.BookingPassengers
                        .Include(p => p.Booking)
                        .Where(p => p.OccurrenceId == request.TripOccurrenceId
                                 && (p.Booking.Status == BookingStatus.Confirmed
                                     || (p.Booking.Status == BookingStatus.Pending
                                         && p.Booking.HoldExpiresAt.HasValue
                                         && p.Booking.HoldExpiresAt.Value > now))
                                 && normalizedRequestedPassengerIds.Contains(p.IdNumber.Trim().ToUpper()))
                        .Select(p => p.IdNumber)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (!string.IsNullOrWhiteSpace(duplicatePassengerId))
                        throw new CartValidationException($"Passenger [{duplicatePassengerId.Trim()}] already holds a ticket for this specific trip.");

                    inventory.RemainingSeats -= requestedSeats;

                    var booking = new Booking
                    {
                        UserId = userId,
                        OccurrenceId = request.TripOccurrenceId,
                        CoachClassId = request.CoachClassId,
                        OriginStationId = request.OriginStationId,
                        DestinationStationId = request.DestinationStationId,
                        SeatsBooked = requestedSeats,
                        TotalPrice = fare.Price * requestedSeats,
                        Status = BookingStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        HoldExpiresAt = DateTime.UtcNow.AddMinutes(10),
                        BookingPassengers = request.Passengers.Select(p => new BookingPassenger
                        {
                            Name = p.Name,
                            Age = p.Age,
                            IdType = p.IdType,
                            IdNumber = p.IdNumber.Trim(),
                            OccurrenceId = request.TripOccurrenceId,
                            CoachClassId = request.CoachClassId,
                            SeatNumber = p.SeatNumber.Trim(),
                            IsOfferedForResale = false
                        }).ToList()
                    };

                    _dbContext.Bookings.Add(booking);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var cart = await GetActiveCartAsync(userId, cancellationToken);
                    return cart ?? new BookingCartResponseDto();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await SafeRollbackAsync(transaction, cancellationToken);
                    throw new CartConcurrencyException("These seats were just purchased by another user. Please search again.", ex);
                }
                catch
                {
                    await SafeRollbackAsync(transaction, cancellationToken);
                    throw;
                }
            });
        }

        public async Task<string> CheckoutAsync(int userId, CheckoutRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(request.PaymentMethod?.Trim(), "Wallet", StringComparison.OrdinalIgnoreCase))
                throw new CartValidationException("Unsupported payment method. Only Wallet payment is currently available.");

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

                try
                {
                    var user = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                    if (user == null)
                        throw new CartValidationException("User not found.");

                    var now = DateTime.UtcNow;
                    var pendingBookings = await QueryActivePendingBookingsForUser(userId, now)
                        .Include(b => b.BookingPassengers)
                        .OrderBy(b => b.BookingTime)
                        .ToListAsync(cancellationToken);

                    if (pendingBookings.Count == 0)
                        throw new CartValidationException("Cart is empty or all items have expired.");

                    var grandTotal = pendingBookings.Sum(b => b.TotalPrice);
                    if (user.WalletBalance < grandTotal)
                        throw new CartValidationException($"Insufficient funds. Your wallet balance is {user.WalletBalance:0.00}, but checkout total is {grandTotal:0.00}.");

                    var seatTracker = new Dictionary<(int OccurrenceId, int CoachClassId), HashSet<string>>();

                    foreach (var booking in pendingBookings)
                    {
                        var inventory = await _dbContext.TripOccurrenceClassInventories
                            .FirstOrDefaultAsync(i => i.TripOccurrenceId == booking.OccurrenceId && i.CoachClassId == booking.CoachClassId, cancellationToken);

                        if (inventory == null)
                            throw new CartValidationException("Inventory data missing.");

                        // Backward compatibility for legacy pending rows created before seat selection support.
                        var hasPlaceholderSeats = booking.BookingPassengers.Any(HasPlaceholderSeat);

                        if (hasPlaceholderSeats)
                        {
                            var key = (booking.OccurrenceId, booking.CoachClassId);
                            if (!seatTracker.TryGetValue(key, out var reservedSeats))
                            {
                                var alreadyTaken = await _dbContext.BookingPassengers
                                    .Where(p => p.OccurrenceId == booking.OccurrenceId
                                             && p.CoachClassId == booking.CoachClassId
                                             && p.BookingId != booking.BookingId
                                             && (p.Booking.Status == BookingStatus.Confirmed
                                                 || (p.Booking.Status == BookingStatus.Pending
                                                     && p.Booking.HoldExpiresAt.HasValue
                                                     && p.Booking.HoldExpiresAt.Value > now)))
                                    .Select(p => p.SeatNumber)
                                    .ToListAsync(cancellationToken);

                                reservedSeats = alreadyTaken.ToHashSet(StringComparer.OrdinalIgnoreCase);
                                seatTracker[key] = reservedSeats;
                            }

                            var availableSeats = Enumerable.Range(1, inventory.TotalSeats)
                                .Select(n => n.ToString())
                                .Where(seat => !reservedSeats.Contains(seat))
                                .ToList();

                            if (availableSeats.Count < booking.BookingPassengers.Count)
                                throw new CartValidationException("Not enough available seats to complete this booking. Please search again.");

                            for (var i = 0; i < booking.BookingPassengers.Count; i++)
                            {
                                var seat = availableSeats[i];
                                booking.BookingPassengers.ElementAt(i).SeatNumber = seat;
                                reservedSeats.Add(seat);
                            }
                        }

                        booking.Status = BookingStatus.Confirmed;
                        booking.PaymentStatus = PaymentStatus.Paid;
                        booking.HoldExpiresAt = null;
                        booking.UpdatedAt = DateTime.UtcNow;
                    }

                    user.WalletBalance -= grandTotal;

                    _dbContext.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = user.UserId,
                        Amount = -grandTotal,
                        Type = TransactionType.TicketPurchase,
                        Description = "Checkout for multiple trips."
                    });

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return $"Checkout successful. {grandTotal:0.00} was deducted from your wallet for {pendingBookings.Count} trip(s).";
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await SafeRollbackAsync(transaction, cancellationToken);
                    throw new CartConcurrencyException("Checkout failed due to concurrent updates. Please try again.", ex);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_BookingPassenger_UniqueSeat", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await SafeRollbackAsync(transaction, cancellationToken);
                    throw new CartConcurrencyException("The selected seats were just taken by another checkout. Please try again.", ex);
                }
                catch
                {
                    await SafeRollbackAsync(transaction, cancellationToken);
                    throw;
                }
            });
        }

        public async Task<BookingCartResponseDto?> GetActiveCartAsync(int userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var bookings = await QueryActivePendingBookingsForUser(userId, now)
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Occurrence)
                    .ThenInclude(o => o.Trip)
                        .ThenInclude(t => t.Agency)
                .Include(b => b.Occurrence)
                    .ThenInclude(o => o.Trip)
                        .ThenInclude(t => t.TripStopTimes)
                .Include(b => b.CoachClass)
                .Include(b => b.BookingPassengers)
                .Include(b => b.OriginStation)
                .Include(b => b.DestinationStation)
                .OrderBy(b => b.HoldExpiresAt)
                .ToListAsync(cancellationToken);

            if (bookings.Count == 0)
                return null;

            var items = bookings.Select(b =>
            {
                var (boardingTime, dropoffTime) = ResolvePassengerLocalTimes(b);

                return new CartItemDto
                {
                    BookingId = b.BookingId,
                    TotalPrice = b.TotalPrice,
                    SeatsBooked = b.SeatsBooked,
                    HoldExpiresAt = AppTime.AsUtc(b.HoldExpiresAt!.Value),
                    AgencyName = b.Occurrence.Trip.Agency.AgencyName,
                    ClassName = b.CoachClass.Name,
                    Origin = b.OriginStation.ArabicName,
                    Destination = b.DestinationStation.ArabicName,
                    BoardingTime = boardingTime,
                    DropoffTime = dropoffTime,
                    Passengers = b.BookingPassengers
                        .Select(p => new TicketPassengerDto
                        {
                            Name = p.Name,
                            IdNumber = p.IdNumber,
                            SeatNumber = p.SeatNumber
                        })
                        .ToList()
                };
            }).ToList();

            return new BookingCartResponseDto
            {
                Items = items,
                GrandTotal = items.Sum(i => i.TotalPrice)
            };
        }

        public async Task<List<MyTicketResponseDto>> GetMyTicketsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var bookings = await _dbContext.Bookings
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Status != BookingStatus.Pending)
                .Include(b => b.Occurrence)
                    .ThenInclude(o => o.Trip)
                        .ThenInclude(t => t.Agency)
                .Include(b => b.Occurrence)
                    .ThenInclude(o => o.Trip)
                        .ThenInclude(t => t.TripStopTimes)
                .Include(b => b.CoachClass)
                .Include(b => b.OriginStation)
                .Include(b => b.DestinationStation)
                .Include(b => b.BookingPassengers)
                .OrderByDescending(b => b.Occurrence.DepartureDateTime)
                .ToListAsync(cancellationToken);

            return bookings.Select(b =>
                {
                    var (boardingTime, dropoffTime) = ResolvePassengerLocalTimes(b);

                    return new MyTicketResponseDto
                    {
                        BookingId = b.BookingId,
                        Status = b.Status.ToString(),
                        PaymentStatus = b.PaymentStatus.ToString(),
                        TotalPrice = b.TotalPrice,
                        SeatsBooked = b.SeatsBooked,
                        BookingDate = AppTime.AsUtc(b.BookingTime),
                        AgencyName = b.Occurrence.Trip.Agency.AgencyName,
                        ClassName = b.CoachClass.Name,
                        OriginStation = b.OriginStation.ArabicName,
                        DestinationStation = b.DestinationStation.ArabicName,
                        BoardingTime = boardingTime,
                        DropoffTime = dropoffTime,
                        Passengers = b.BookingPassengers
                        .Select(p => new TicketPassengerDto
                        {
                            Name = p.Name,
                            IdNumber = p.IdNumber,
                            SeatNumber = p.SeatNumber
                        })
                        .ToList()
                    };
                })
                .ToList();
        }

        public async Task ReleaseExpiredHoldsAsync(CancellationToken cancellationToken = default)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

                try
                {
                    var now = DateTime.UtcNow;

                    var expiredBookings = await _dbContext.Bookings
                        .Include(b => b.BookingPassengers)
                        .Where(b => b.Status == BookingStatus.Pending
                                 && b.HoldExpiresAt.HasValue
                                 && b.HoldExpiresAt.Value <= now)
                        .ToListAsync(cancellationToken);

                    if (expiredBookings.Count == 0)
                    {
                        await transaction.CommitAsync(cancellationToken);
                        return;
                    }

                    var groupedExpiredBookings = expiredBookings
                        .GroupBy(b => new { b.OccurrenceId, b.CoachClassId })
                        .ToList();

                    var occurrenceIds = groupedExpiredBookings
                        .Select(g => g.Key.OccurrenceId)
                        .Distinct()
                        .ToList();

                    var coachClassIds = groupedExpiredBookings
                        .Select(g => g.Key.CoachClassId)
                        .Distinct()
                        .ToList();

                    var inventories = await _dbContext.TripOccurrenceClassInventories
                        .Where(i => occurrenceIds.Contains(i.TripOccurrenceId)
                                 && coachClassIds.Contains(i.CoachClassId))
                        .ToListAsync(cancellationToken);

                    var inventoryLookup = inventories
                        .ToDictionary(i => (i.TripOccurrenceId, i.CoachClassId));

                    foreach (var group in groupedExpiredBookings)
                    {
                        if (inventoryLookup.TryGetValue((group.Key.OccurrenceId, group.Key.CoachClassId), out var inventory))
                        {
                            var seatsToRestore = group.Sum(b => b.SeatsBooked);
                            inventory.RemainingSeats = Math.Min(inventory.TotalSeats, inventory.RemainingSeats + seatsToRestore);
                        }

                        foreach (var booking in group)
                        {
                            booking.Status = BookingStatus.Cancelled;
                            booking.UpdatedAt = now;
                        }
                    }

                    var expiredPassengers = expiredBookings
                        .SelectMany(b => b.BookingPassengers)
                        .ToList();

                    if (expiredPassengers.Count > 0)
                    {
                        _dbContext.BookingPassengers.RemoveRange(expiredPassengers);
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await SafeRollbackAsync(transaction, cancellationToken);
                    throw;
                }
            });
        }

        public async Task ProcessCompletedTripsAsync(CancellationToken cancellationToken = default)
        {
            var now = AppTime.GetScheduleNow();

            var completedCandidates = await _dbContext.Bookings
                .Include(b => b.User)
                .Include(b => b.Occurrence)
                .Where(b => b.Status == BookingStatus.Confirmed
                         && b.Occurrence.ArrivalDateTime <= now)
                .ToListAsync(cancellationToken);

            if (completedCandidates.Count == 0)
                return;

            foreach (var booking in completedCandidates)
            {
                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = now;

                // Existing domain field for completed trips count
                booking.User.TotalTripsCount += 1;
                booking.User.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<Booking> QueryActivePendingBookingsForUser(int userId, DateTime now)
        {
            return _dbContext.Bookings.Where(b => b.UserId == userId
                                               && b.Status == BookingStatus.Pending
                                               && b.HoldExpiresAt.HasValue
                                               && b.HoldExpiresAt.Value > now);
        }

        private static List<string> GetRequestedSeatNumbers(AddToCartRequestDto request)
        {
            return request.Passengers
                .Select(p => (p.SeatNumber ?? string.Empty).Trim())
                .ToList();
        }

        private static List<string> GetRequestedPassengerIds(AddToCartRequestDto request)
        {
            return request.Passengers
                .Select(p => (p.IdNumber ?? string.Empty).Trim())
                .ToList();
        }

        private static List<string> NormalizePassengerIds(IEnumerable<string> passengerIds)
        {
            return passengerIds
                .Select(id => id.ToUpperInvariant())
                .Distinct()
                .ToList();
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

        private static DateTime BuildSegmentDateTime(DateTime occurrenceStart, TimeOnly tripOriginDeparture, TimeOnly segmentTime)
        {
            var offset = segmentTime.ToTimeSpan() - tripOriginDeparture.ToTimeSpan();
            if (offset < TimeSpan.Zero)
                offset = offset.Add(TimeSpan.FromDays(1));

            return occurrenceStart.Add(offset);
        }

        private static bool HasPlaceholderSeat(BookingPassenger passenger)
        {
            return string.IsNullOrWhiteSpace(passenger.SeatNumber)
                   || passenger.SeatNumber.StartsWith("PENDING-", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task SafeRollbackAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch
            {
                // No-op by design; rollback best-effort during exception handling.
            }
        }
    }
}
