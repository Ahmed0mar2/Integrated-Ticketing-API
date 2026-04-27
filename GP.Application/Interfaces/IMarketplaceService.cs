using GP.Application.Common;
using GP.Application.DTOs.Marketplace;

namespace GP.Application.Interfaces;

public interface IMarketplaceService
{
    Task<ApiResponse> ListTicketAsync(int sellerUserId, ListTicketRequestDto request, CancellationToken cancellationToken = default);

    Task<ApiResponse> BuyTicketAsync(int buyerUserId, int listingId, CancellationToken cancellationToken = default);

    Task<PagedResult<MarketplaceListingResponseDto>> GetActiveListingsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
