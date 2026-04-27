using GP.Application.Common;
using GP.Application.DTOs.Marketplace;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GP.Application.Services;

public class MarketplaceService : IMarketplaceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MarketplaceService> _logger;

    public MarketplaceService(
        ApplicationDbContext dbContext,
        ILogger<MarketplaceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse> ListTicketAsync(int sellerUserId, ListTicketRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            return ApiResponse.Fail("Invalid request payload.");

        if (request.AskingPrice <= 0)
            return ApiResponse.Fail("Asking price must be greater than zero.");

        var booking = await _dbContext.Bookings
            .Include(b => b.BookingPassengers)
            .Include(b => b.Occurrence)
            .FirstOrDefaultAsync(b => b.BookingId == request.BookingId, cancellationToken);

        if (booking == null)
            return ApiResponse.Fail("Booking not found.");

        if (booking.UserId != sellerUserId)
            return ApiResponse.Fail("You can only list tickets from your own booking.");

        if (booking.Status != BookingStatus.Confirmed)
            return ApiResponse.Fail("Only confirmed tickets can be listed on the marketplace.");

        var passenger = booking.BookingPassengers
            .FirstOrDefault(p => p.PassengerId == request.PassengerId);

        if (passenger == null)
            return ApiResponse.Fail("Passenger ticket not found in this booking.");

        if (passenger.IsOfferedForResale)
            return ApiResponse.Fail("Ticket is already on the marketplace.");

        var scheduleNow = AppTime.GetScheduleNow();
        if (booking.Occurrence.DepartureDateTime <= scheduleNow)
            return ApiResponse.Fail("Ticket can no longer be listed because trip departure has passed.");

        var originalPrice = booking.SeatsBooked > 0
            ? booking.TotalPrice / booking.SeatsBooked
            : booking.TotalPrice;

        var listing = new MarketplaceListing
        {
            BookingId = booking.BookingId,
            PassengerId = passenger.PassengerId,
            SellerId = sellerUserId,
            OriginalPrice = originalPrice,
            AskingPrice = request.AskingPrice,
            Status = ListingStatus.Available,
            ListedAt = DateTime.UtcNow
        };

        _dbContext.MarketplaceListings.Add(listing);
        passenger.IsOfferedForResale = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marketplace listing created. BookingId: {BookingId}, PassengerId: {PassengerId}, SellerId: {SellerId}",
            booking.BookingId, passenger.PassengerId, sellerUserId);

        return ApiResponse.Ok("Ticket listed on marketplace successfully.");
    }

    public async Task<ApiResponse> BuyTicketAsync(int buyerUserId, int listingId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var listing = await _dbContext.MarketplaceListings
                .Include(l => l.Booking)
                    .ThenInclude(b => b.BookingPassengers)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Occurrence)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken);

            if (listing == null)
                return ApiResponse.Fail("Marketplace listing not found.");

            if (listing.Status != ListingStatus.Available)
                return ApiResponse.Fail("Listing is no longer available.");

            if (buyerUserId == listing.SellerId)
                return ApiResponse.Fail("You cannot buy your own listed ticket.");

            if (listing.Booking.Occurrence.DepartureDateTime <= AppTime.GetScheduleNow())
                return ApiResponse.Fail("This ticket can no longer be purchased because trip departure has passed.");

            var buyer = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == buyerUserId, cancellationToken);

            if (buyer == null)
                return ApiResponse.Fail("Buyer account not found.");

            if (buyer.WalletBalance < listing.AskingPrice)
                return ApiResponse.Fail("Insufficient wallet balance to purchase this ticket.");

            var oldBooking = listing.Booking;
            if (oldBooking.Status != BookingStatus.Confirmed)
                return ApiResponse.Fail("Source booking is not eligible for resale transfer.");

            var passenger = oldBooking.BookingPassengers
                .FirstOrDefault(p => p.PassengerId == listing.PassengerId);

            if (passenger == null)
                return ApiResponse.Fail("The listed passenger ticket could not be found.");

            // Wallet transfer
            buyer.WalletBalance -= listing.AskingPrice;
            listing.Seller.WalletBalance += listing.AskingPrice;

            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                UserId = buyerUserId,
                Amount = -listing.AskingPrice,
                Type = TransactionType.TicketPurchase,
                Description = "Purchased ticket from marketplace",
                BookingId = null
            });

            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                UserId = listing.SellerId,
                Amount = listing.AskingPrice,
                Type = TransactionType.Deposit,
                Description = "Ticket sold on marketplace",
                BookingId = null
            });

            // Booking split (ownership transfer)
            var newBooking = new Booking
            {
                UserId = buyerUserId,
                OccurrenceId = oldBooking.OccurrenceId,
                CoachClassId = oldBooking.CoachClassId,
                OriginStationId = oldBooking.OriginStationId,
                DestinationStationId = oldBooking.DestinationStationId,
                SeatsBooked = 1,
                TotalPrice = listing.AskingPrice,
                Status = BookingStatus.Confirmed,
                PaymentStatus = PaymentStatus.Paid,
                HoldExpiresAt = null,
                BookingPassengers = [passenger]
            };

            _dbContext.Bookings.Add(newBooking);

            oldBooking.SeatsBooked -= 1;
            if (oldBooking.SeatsBooked < 0)
                oldBooking.SeatsBooked = 0;

            passenger.IsOfferedForResale = false;

            listing.Status = ListingStatus.Sold;
            listing.SoldAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Marketplace listing sold. ListingId: {ListingId}, BuyerId: {BuyerId}, SellerId: {SellerId}",
                listingId, buyerUserId, listing.SellerId);

            return ApiResponse.Ok("Ticket purchased successfully.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<MarketplaceListingResponseDto>> GetActiveListingsAsync(
        int pageNumber,
        int pageSize,
        MarketplaceSearchRequestDto searchDto,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, pageSize);

        var scheduleNow = AppTime.GetScheduleNow();

        var query = _dbContext.MarketplaceListings
            .AsNoTracking()
            .Include(l => l.Booking)
                .ThenInclude(b => b.Occurrence)
                    .ThenInclude(o => o.Trip)
                        .ThenInclude(t => t.Agency)
            .Include(l => l.Booking)
                .ThenInclude(b => b.Occurrence)
                    .ThenInclude(o => o.Trip)
                        .ThenInclude(t => t.TripStopTimes)
            .Include(l => l.Booking)
                .ThenInclude(b => b.OriginStation)
            .Include(l => l.Booking)
                .ThenInclude(b => b.DestinationStation)
            .Include(l => l.Booking)
                .ThenInclude(b => b.CoachClass)
            .Include(l => l.Seller)
            .Where(l => l.Status == ListingStatus.Available)
            .Where(l => l.Booking.Occurrence.DepartureDateTime > scheduleNow);

        if (searchDto.OriginStationId.HasValue)
        {
            query = query.Where(l => l.Booking.OriginStationId == searchDto.OriginStationId.Value);
        }

        if (searchDto.DestinationStationId.HasValue)
        {
            query = query.Where(l => l.Booking.DestinationStationId == searchDto.DestinationStationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.OriginGovernorate))
        {
            query = query.Where(l => l.Booking.OriginStation.Governorate == searchDto.OriginGovernorate);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.DestinationGovernorate))
        {
            query = query.Where(l => l.Booking.DestinationStation.Governorate == searchDto.DestinationGovernorate);
        }

        if (searchDto.TravelDate.HasValue)
        {
            query = query.Where(l => l.Booking.Occurrence.DepartureDateTime.Date == searchDto.TravelDate.Value.Date);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var listings = await query
            .OrderBy(l => l.ListedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = listings.Select(l =>
        {
            var booking = l.Booking;
            var (boardingTime, _) = ResolvePassengerLocalTimes(booking);

            return new MarketplaceListingResponseDto
            {
                ListingId = l.Id,
                OriginalPrice = l.OriginalPrice,
                AskingPrice = l.AskingPrice,
                SellerName = BuildSellerFullName(l.Seller),
                TripDetails = new MarketplaceTripDetailsDto
                {
                    Origin = booking.OriginStation.ArabicName,
                    Destination = booking.DestinationStation.ArabicName,
                    Time = boardingTime,
                    Class = $"{booking.Occurrence.Trip.Agency.AgencyName} - {booking.CoachClass.Name}"
                }
            };
        }).ToList();

        return new PagedResult<MarketplaceListingResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            PageSize = pageSize
        };
    }

    private static string BuildSellerFullName(User seller)
    {
        return $"{seller.FirstName} {seller.FamilyName} {seller.LastName}".Trim();
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

        return (AppTime.AsSchedule(boardingTime), AppTime.AsSchedule(dropoffTime));
    }

    private static DateTime BuildSegmentDateTime(DateTime occurrenceStart, TimeOnly tripOriginDeparture, TimeOnly segmentTime)
    {
        var offset = segmentTime.ToTimeSpan() - tripOriginDeparture.ToTimeSpan();
        if (offset < TimeSpan.Zero)
            offset = offset.Add(TimeSpan.FromDays(1));

        return occurrenceStart.Add(offset);
    }

    private static async Task SafeRollbackAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {

        }
    }
}
