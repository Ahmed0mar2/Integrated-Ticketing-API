using GP.Application.DTOs.Bookings;

namespace GP.Application.Interfaces
{
    public interface IBoardingPassService
    {
        Task<string> GenerateBoardingPassPayloadAsync(
            int userId,
            int bookingId,
            int passengerId,
            CancellationToken cancellationToken = default);

        Task<VerifyPassResponseDto> VerifyBoardingPassAsync(
            string payload,
            CancellationToken cancellationToken = default);
    }
}
