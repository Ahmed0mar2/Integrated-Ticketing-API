namespace GP.Application.DTOs.Marketplace;

public class MarketplaceSearchRequestDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? OriginStationId { get; set; }
    public int? DestinationStationId { get; set; }
    public string? OriginGovernorate { get; set; }
    public string? DestinationGovernorate { get; set; }
    public DateTime? TravelDate { get; set; }
}
