using GP.Application.Common;

namespace GP.Application.Interfaces
{
    public interface ILoyaltyService
    {
        Task<ApiResponse> ExpireOldPointsAsync(CancellationToken cancellationToken = default);
        Task<bool> DeductPointsFifoAsync(int userId, int pointsToDeduct, string description, int? bookingId = null, CancellationToken cancellationToken = default);
        Task<ApiResponse> ResetMonthlyChallengesAsync(CancellationToken cancellationToken = default);
    }
}
