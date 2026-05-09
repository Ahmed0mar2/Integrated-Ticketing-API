using GP.Application.DTOs.Bookings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingCartResponseDto> AddToCartAsync(int userId, AddToCartRequestDto request, CancellationToken cancellationToken = default);
        Task<string> CheckoutAsync(int userId, CheckoutRequestDto request, CancellationToken cancellationToken = default);
        Task<BookingCartResponseDto?> GetActiveCartAsync(int userId, CancellationToken cancellationToken = default);
        Task CancelCartHoldAsync(int userId, int bookingId, CancellationToken cancellationToken = default);
        Task<List<MyTicketResponseDto>> GetMyTicketsAsync(int userId, CancellationToken cancellationToken = default);
        Task ReleaseExpiredHoldsAsync(CancellationToken cancellationToken = default);
        Task ProcessCompletedTripsAsync(CancellationToken cancellationToken = default);
        Task ProcessUpcomingBoardingAlertsAsync(CancellationToken cancellationToken = default);
    }
}
