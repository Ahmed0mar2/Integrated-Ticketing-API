using GP.Application.DTOs.Bookings;

namespace GP.Application.DTOs.Marketplace;

public class MarketplaceBuyRequestDto
{
    public List<PassengerDetailDto> Passengers { get; set; } = new();
}
