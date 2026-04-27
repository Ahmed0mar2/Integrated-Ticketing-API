namespace GP.Application.DTOs.Marketplace;

public class MarketplaceSearchRequestDto
{
    public int? OriginStationId { get; set; }
    public int? DestinationStationId { get; set; }
    public string? OriginGovernorate { get; set; }
    public string? DestinationGovernorate { get; set; }
    public DateTime? TravelDate { get; set; }
}
