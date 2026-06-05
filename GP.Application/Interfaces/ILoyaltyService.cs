using GP.Application.Common;
using GP.Application.DTOs.Loyalty;

namespace GP.Application.Interfaces
{
    public interface ILoyaltyService
    {
        Task<ApiResponse> ExpireOldPointsAsync(CancellationToken cancellationToken = default);
        Task<bool> DeductPointsFifoAsync(int userId, int pointsToDeduct, string description, string descriptionAr, int? bookingId = null, CancellationToken cancellationToken = default);
        Task<ApiResponse> ResetMonthlyChallengesAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<GP.Application.DTOs.Loyalty.PointTransactionHistoryDto>> GetUserPointHistoryAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResult<UserChallengeHistoryDto>> GetUserChallengesAsync(int userId, bool? isCompleted, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
