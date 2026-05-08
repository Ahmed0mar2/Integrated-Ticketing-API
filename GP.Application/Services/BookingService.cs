using GP.Application.Common;
using GP.Application.DTOs.Bookings;
using GP.Application.Events;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace GP.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;

        public BookingService(
            ApplicationDbContext dbContext,
            ILoyaltyService loyaltyService,
            IMediator mediator,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _loyaltyService = loyaltyService;
            _mediator = mediator;
            _configuration = configuration;
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

                    if (string.IsNullOrWhiteSpace(request.ContactName)
                        || string.IsNullOrWhiteSpace(request.ContactPhone)
                        || string.IsNullOrWhiteSpace(request.ContactEmail))
                    {
                        throw new CartValidationException("ContactName, ContactPhone, and ContactEmail are required.");
                    }

                    var tripContext = await _dbContext.TripOccurrences
                        .AsNoTracking()
                        .Where(o => o.TripOccurrenceId == request.TripOccurrenceId)
                        .Select(o => new
                        {
                            o.TripId,
                            AgencyName = o.Trip.Agency.AgencyName
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (tripContext == null)
                        throw new CartValidationException("Trip occurrence not found.");

                    var isEnrAgency = string.Equals(
                        tripContext.AgencyName,
                        "Egyptian National Railways",
                        StringComparison.OrdinalIgnoreCase);

                    var requestedSeats = request.Passengers.Count;

                    var inventory = await _dbContext.TripOccurrenceClassInventories
                        .FirstOrDefaultAsync(i => i.TripOccurrenceId == request.TripOccurrenceId
                                               && i.CoachClassId == request.CoachClassId, cancellationToken);

                    if (inventory == null)
                        throw new CartValidationException("Class inventory not found for this trip.");

                    if (inventory.RemainingSeats < requestedSeats)
                        throw new CartValidationException("Not enough capacity.");

                    var now = AppTime.GetScheduleNow();

                    List<string> resolvedSeatNumbers;
                    var parsedPassengerIdTypes = new List<IdType>();

                    if (isEnrAgency)
                    {
                        if (request.Passengers.Any(p => string.IsNullOrWhiteSpace(p.PassengerName)))
                            throw new CartValidationException("PassengerName is required for every ENR passenger.");

                        if (request.Passengers.Any(p => string.IsNullOrWhiteSpace(p.IdType)))
                            throw new CartValidationException("IdType is required for every ENR passenger.");

                        if (request.Passengers.Any(p => string.IsNullOrWhiteSpace(p.IdNumber)))
                            throw new CartValidationException("IdNumber is required for every ENR passenger.");

                        var requestedPassengerIds = GetRequestedPassengerIds(request);
                        var normalizedRequestedPassengerIds = NormalizePassengerIds(requestedPassengerIds);

                        if (normalizedRequestedPassengerIds.Count != requestedPassengerIds.Count)
                            throw new CartValidationException("Passenger ID numbers must be unique within the same cart item.");

                        parsedPassengerIdTypes = request.Passengers
                            .Select((p, index) => ParsePassengerIdTypeOrThrow(p.IdType!, index + 1))
                            .ToList();

                        var duplicatePassengerId = await _dbContext.BookingPassengers
                            .Include(p => p.Booking)
                            .Where(p => p.OccurrenceId == request.TripOccurrenceId
                                     && p.IdNumber != null
                                     && (p.Booking.Status == BookingStatus.Confirmed
                                         || (p.Booking.Status == BookingStatus.Pending
                                             && p.Booking.HoldExpiresAt.HasValue
                                             && p.Booking.HoldExpiresAt.Value > now))
                                     && normalizedRequestedPassengerIds.Contains(p.IdNumber.Trim().ToUpper()))
                            .Select(p => p.IdNumber)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (!string.IsNullOrWhiteSpace(duplicatePassengerId))
                            throw new CartValidationException($"Passenger [{duplicatePassengerId.Trim()}] already holds a ticket for this specific trip.");

                        resolvedSeatNumbers = await AssignNextAvailableSeatNumbersAsync(
                            request.TripOccurrenceId,
                            request.CoachClassId,
                            requestedSeats,
                            inventory.TotalSeats,
                            now,
                            cancellationToken);
                    }
                    else
                    {
                        var requestedSeatNumbers = GetRequestedSeatNumbers(request);

                        if (requestedSeatNumbers.Any(string.IsNullOrWhiteSpace))
                            throw new CartValidationException("Seat number is required for each passenger.");

                        if (requestedSeatNumbers.Count != requestedSeatNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                            throw new CartValidationException("Seat numbers must be unique within the same cart item.");

                        var outOfRangeSeats = requestedSeatNumbers
                            .Where(seat => !int.TryParse(seat, out var seatNo) || seatNo <= 0 || seatNo > inventory.TotalSeats)
                            .ToList();

                        if (outOfRangeSeats.Count > 0)
                            throw new CartValidationException("One or more selected seats are invalid for this class.");

                        var lockedSeatNumbers = await GetLockedSeatNumbersAsync(
                            request.TripOccurrenceId,
                            request.CoachClassId,
                            now,
                            cancellationToken);

                        var conflictingSeats = requestedSeatNumbers
                            .Intersect(lockedSeatNumbers, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        if (conflictingSeats.Count > 0)
                            throw new CartConcurrencyException("One or more selected seats were just taken. Please refresh the seat map.");

                        resolvedSeatNumbers = requestedSeatNumbers;
                    }

                    var fare = await _dbContext.TripFares
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f => f.TripId == tripContext.TripId
                                               && f.OriginStationId == request.OriginStationId
                                               && f.DestinationStationId == request.DestinationStationId
                                               && f.CoachClassId == request.CoachClassId, cancellationToken);

                    if (fare == null)
                        throw new CartValidationException("Pricing not found for this route and class.");

                    inventory.RemainingSeats -= requestedSeats;

                    var normalizedContactName = request.ContactName.Trim();
                    var normalizedContactPhone = request.ContactPhone.Trim();
                    var normalizedContactEmail = request.ContactEmail.Trim();

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
                        HoldExpiresAt = now.AddMinutes(10),
                        ContactName = normalizedContactName,
                        ContactPhone = normalizedContactPhone,
                        ContactEmail = normalizedContactEmail,
                        BookingPassengers = request.Passengers.Select((p, index) => new BookingPassenger
                        {
                            Name = isEnrAgency ? p.PassengerName!.Trim() : normalizedContactName,
                            IdType = isEnrAgency ? parsedPassengerIdTypes[index] : null,
                            IdNumber = isEnrAgency
                                ? p.IdNumber!.Trim()
                                : null,
                            OccurrenceId = request.TripOccurrenceId,
                            CoachClassId = request.CoachClassId,
                            SeatNumber = resolvedSeatNumbers[index],
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

        public async Task CancelCartHoldAsync(int userId, int bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.BookingPassengers)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId, cancellationToken);

            if (booking == null || booking.Status != BookingStatus.Pending)
                throw new CartValidationException("Pending booking was not found.");

            var inventory = await _dbContext.TripOccurrenceClassInventories
                .FirstOrDefaultAsync(i => i.TripOccurrenceId == booking.OccurrenceId
                                       && i.CoachClassId == booking.CoachClassId, cancellationToken);

            if (inventory != null)
            {
                inventory.RemainingSeats = Math.Min(
                    inventory.TotalSeats,
                    inventory.RemainingSeats + booking.BookingPassengers.Count);
            }

            _dbContext.Bookings.Remove(booking);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<string> CheckoutAsync(int userId, CheckoutRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(request.PaymentMethod?.Trim(), "Wallet", StringComparison.OrdinalIgnoreCase))
                throw new CartValidationException("Unsupported payment method. Only Wallet payment is currently available.");

            decimal pointToEgpValue = _configuration.GetValue<decimal>("LoyaltySettings:PointToEgpValue", 0.05m);
            decimal maxDiscountPct = _configuration.GetValue<decimal>("LoyaltySettings:MaxDiscountPercentage", 0.50m);
            decimal earnRate = _configuration.GetValue<decimal>("LoyaltySettings:EarnRatePercentage", 0.05m);

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

                    var now = AppTime.GetScheduleNow();
                    var pendingBookings = await QueryActivePendingBookingsForUser(userId, now)
                        .Include(b => b.Occurrence)
                        .Include(b => b.BookingPassengers)
                        .OrderBy(b => b.BookingTime)
                        .ToListAsync(cancellationToken);

                    if (pendingBookings.Count == 0)
                        throw new CartValidationException("Cart is empty or all items have expired.");

                    var grandTotal = pendingBookings.Sum(b => b.TotalPrice);
                    decimal discountEgp = 0m;
                    if (request.PointsToRedeem > 0)
                    {
                        decimal requestedDiscount = request.PointsToRedeem * pointToEgpValue;
                        decimal maxDiscount = grandTotal * maxDiscountPct;
                        discountEgp = Math.Min(requestedDiscount, maxDiscount);

                        int actualPointsToDeduct = (int)(discountEgp / pointToEgpValue);

                        await _loyaltyService.DeductPointsFifoAsync(userId, actualPointsToDeduct, "Ticket Discount", null, cancellationToken);
                    }

                    decimal finalPrice = Math.Max(grandTotal - discountEgp, 10.00m);

                    if (user.WalletBalance < finalPrice)
                        throw new CartValidationException($"Insufficient funds. Your wallet balance is {user.WalletBalance:0.00}, but checkout total is {finalPrice:0.00}.");

                    var appliedDiscount = grandTotal - finalPrice;
                    if (appliedDiscount > 0m && grandTotal > 0m)
                    {
                        var remainingDiscount = appliedDiscount;

                        for (var i = 0; i < pendingBookings.Count; i++)
                        {
                            var booking = pendingBookings[i];

                            if (i == pendingBookings.Count - 1)
                            {
                                booking.TotalPrice = Math.Max(0m, booking.TotalPrice - remainingDiscount);
                                break;
                            }

                            var proportional = Math.Round(booking.TotalPrice / grandTotal * appliedDiscount, 2, MidpointRounding.AwayFromZero);
                            proportional = Math.Min(proportional, remainingDiscount);
                            booking.TotalPrice = Math.Max(0m, booking.TotalPrice - proportional);
                            remainingDiscount -= proportional;
                        }
                    }

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
                        booking.UpdatedAt = now;
                    }

                    user.WalletBalance -= finalPrice;

                    _dbContext.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = user.UserId,
                        Amount = -finalPrice,
                        Type = TransactionType.TicketPurchase,
                        Description = "Checkout for multiple trips."
                    });

                    int distinctTrips = pendingBookings.Select(c => c.OccurrenceId).Distinct().Count();
                    decimal bonusMultiplier = distinctTrips switch
                    {
                        >= 3 => 1.25m,
                        2 => 1.15m,
                        _ => 1.00m
                    };

                    int earnedPoints = (int)(finalPrice * earnRate * bonusMultiplier);
                    var departureDate = pendingBookings.Min(c => c.Occurrence.DepartureDateTime);
                    var referenceBookingId = pendingBookings[0].BookingId;

                    var earnTransaction = new PointTransaction
                    {
                        UserId = userId,
                        Amount = earnedPoints,
                        AvailableAmount = earnedPoints,
                        Description = $"Earned from {distinctTrips}-leg Booking",
                        Source = PointSource.BookingEarned,
                        Status = PointTransactionStatus.Pending,
                        CreatedAt = now,
                        UnlocksAt = departureDate,
                        BookingId = referenceBookingId,
                        ExpiresAt = departureDate.AddMonths(4)
                    };
                    _dbContext.PointTransactions.Add(earnTransaction);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return $"Checkout successful. {finalPrice:0.00} was deducted from your wallet for {pendingBookings.Count} trip(s).";
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
            var now = AppTime.GetScheduleNow();

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
                    HoldExpiresAt = AppTime.AsSchedule(b.HoldExpiresAt!.Value),
                    AgencyName = b.Occurrence.Trip.Agency.AgencyName,
                    ClassName = b.CoachClass.Name,
                    Origin = b.OriginStation.ArabicName,
                    OriginGov = b.OriginStation.Governorate ?? "Unknown",
                    Destination = b.DestinationStation.ArabicName,
                    DestinationGov = b.DestinationStation.Governorate ?? "Unknown",
                    BoardingTime = boardingTime,
                    DropoffTime = dropoffTime,
                    Passengers = b.BookingPassengers
                        .Select(p => new TicketPassengerDto
                        {
                            PassengerId = p.PassengerId,
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

            var activeListingLookup = new Dictionary<int, int?>();
            if (bookings.Count > 0)
            {
                var bookingIds = bookings.Select(b => b.BookingId).ToList();

                activeListingLookup = await _dbContext.MarketplaceListings
                    .AsNoTracking()
                    .Where(l => bookingIds.Contains(l.BookingId) && l.Status == ListingStatus.Available)
                    .GroupBy(l => l.BookingId)
                    .Select(g => new
                    {
                        BookingId = g.Key,
                        ListingId = g.OrderByDescending(x => x.ListedAt)
                            .Select(x => x.Id)
                            .First()
                    })
                    .ToDictionaryAsync(x => x.BookingId, x => (int?)x.ListingId, cancellationToken);
            }

            return bookings.Select(b =>
                {
                    var (boardingTime, dropoffTime) = ResolvePassengerLocalTimes(b);

                    activeListingLookup.TryGetValue(b.BookingId, out var activeListingId);

                    return new MyTicketResponseDto
                    {
                        BookingId = b.BookingId,
                        Status = b.Status.ToString(),
                        PaymentStatus = b.PaymentStatus.ToString(),
                        TotalPrice = b.TotalPrice,
                        SeatsBooked = b.SeatsBooked,
                        BookingDate = AppTime.AsSchedule(b.BookingTime),
                        IsMarketplacePurchase = b.IsMarketplacePurchase,
                        ActiveListingId = activeListingId,
                        IsOfferedForResale = activeListingId.HasValue,
                        AgencyName = b.Occurrence.Trip.Agency.AgencyName,
                        ClassName = b.CoachClass.Name,
                        OriginStation = b.OriginStation.ArabicName,
                        OriginGov = b.OriginStation.Governorate ?? "Unknown",
                        DestinationStation = b.DestinationStation.ArabicName,
                        DestinationGov = b.DestinationStation.Governorate ?? "Unknown",
                        BoardingTime = boardingTime,
                        DropoffTime = dropoffTime,
                        Passengers = b.BookingPassengers
                        .Select(p => new TicketPassengerDto
                        {
                            PassengerId = p.PassengerId,
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
                    var now = AppTime.GetScheduleNow();

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

            foreach (var booking in completedCandidates)
            {
                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = now;

                // Existing domain field for completed trips count
                booking.User.TotalTripsCount += 1;
                booking.User.UpdatedAt = now;
            }

            var pendingPoints = await _dbContext.PointTransactions
                .Where(pt => pt.Status == PointTransactionStatus.Pending
                             && pt.UnlocksAt.HasValue
                             && pt.UnlocksAt <= now)
                .ToListAsync(cancellationToken);

            foreach (var pt in pendingPoints)
            {
                pt.Status = PointTransactionStatus.Available;
            }

            var unlockedPointsByUser = pendingPoints
                .GroupBy(pt => pt.UserId)
                .Select(g => new { UserId = g.Key, TotalUnlocked = g.Sum(pt => pt.Amount) });

            foreach (var userPoints in unlockedPointsByUser)
            {
                var user = await _dbContext.Users.FindAsync(new object[] { userPoints.UserId }, cancellationToken);
                if (user != null)
                {
                    // 1. Give them their earned points
                    user.LoyaltyPointsBalance += userPoints.TotalUnlocked;

                    // 2. Calculate gamification metrics for the trips that just arrived
                    var userFinishedTrips = pendingPoints
                        .Where(p => p.UserId == userPoints.UserId && p.BookingId.HasValue)
                        .ToList();

                    if (userFinishedTrips.Count > 0)
                    {
                        int completedLegs = userFinishedTrips.Count;
                        decimal totalSpendForTheseTrips = 0;

                        foreach (var pt in userFinishedTrips)
                        {
                            var booking = await _dbContext.Bookings.FindAsync(new object[] { pt.BookingId!.Value }, cancellationToken);
                            if (booking != null)
                            {
                                // Add the price to the gamification tracker
                                totalSpendForTheseTrips += booking.TotalPrice;
                            }
                        }

                        // 3. Fire the event safely! Progress is now strictly tied to arrival.
                        await _mediator.Publish(new BookingCompletedEvent(user.UserId, completedLegs, totalSpendForTheseTrips), cancellationToken);
                    }
                }
            }

            if (completedCandidates.Count == 0 && pendingPoints.Count == 0)
                return;

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

        private async Task<List<string>> GetLockedSeatNumbersAsync(
            int tripOccurrenceId,
            int coachClassId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            return await _dbContext.BookingPassengers
                .Where(p => p.OccurrenceId == tripOccurrenceId
                         && p.CoachClassId == coachClassId)
                .Where(p => p.Booking.Status == BookingStatus.Confirmed
                         || (p.Booking.Status == BookingStatus.Pending
                             && p.Booking.HoldExpiresAt.HasValue
                             && p.Booking.HoldExpiresAt.Value > now))
                .Select(p => p.SeatNumber)
                .ToListAsync(cancellationToken);
        }

        private async Task<List<string>> AssignNextAvailableSeatNumbersAsync(
            int tripOccurrenceId,
            int coachClassId,
            int seatsNeeded,
            int capacity,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var lockedSeatNumbers = await GetLockedSeatNumbersAsync(tripOccurrenceId, coachClassId, now, cancellationToken);

            var reservedSeatNumbers = lockedSeatNumbers
                .Select(seat => int.TryParse(seat, out var number) ? number : (int?)null)
                .Where(number => number.HasValue && number.Value > 0 && number.Value <= capacity)
                .Select(number => number!.Value)
                .ToHashSet();

            var nextAvailable = Enumerable.Range(1, capacity)
                .Where(seat => !reservedSeatNumbers.Contains(seat))
                .Take(seatsNeeded)
                .Select(seat => seat.ToString())
                .ToList();

            if (nextAvailable.Count < seatsNeeded)
                throw new CartValidationException("Not enough capacity.");

            return nextAvailable;
        }

        private static IdType ParsePassengerIdTypeOrThrow(string rawIdType, int passengerIndex)
        {
            if (Enum.TryParse<IdType>(rawIdType.Trim(), true, out var parsed) && Enum.IsDefined(parsed))
                return parsed;

            throw new CartValidationException($"Invalid IdType for passenger #{passengerIndex}. Allowed values: NationalId, Passport, DrivingLicense, StudentId, Other.");
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
