using GP.Application.Common;
using GP.Application.DTOs.Marketplace;

namespace GP.Application.Interfaces;

public interface IMarketplaceService
{
    Task<ApiResponse> ListTicketAsync(int sellerUserId, ListTicketRequestDto request, CancellationToken cancellationToken = default);

    Task<ApiResponse> BuyTicketAsync(int buyerUserId, int listingId, CancellationToken cancellationToken = default);

    Task<ApiResponse> CancelListingAsync(int userId, int listingId, CancellationToken cancellationToken = default);
    Task<ApiResponse> CancelListingByBookingAsync(int userId, int bookingId, CancellationToken cancellationToken = default);

    Task<PagedResult<MarketplaceListingResponseDto>> GetActiveListingsAsync(
        int pageNumber,
        int pageSize,
        MarketplaceSearchRequestDto searchDto,
        CancellationToken cancellationToken = default);
}
