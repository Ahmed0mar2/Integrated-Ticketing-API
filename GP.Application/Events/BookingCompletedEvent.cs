using MediatR;

namespace GP.Application.Events
{
    public record BookingCompletedEvent(int UserId, int DistinctTripsCount, decimal FinalPrice) : INotification;
}
