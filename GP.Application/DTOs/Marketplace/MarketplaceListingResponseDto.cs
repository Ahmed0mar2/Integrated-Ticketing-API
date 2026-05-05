namespace GP.Application.DTOs.Marketplace;

public class MarketplaceListingResponseDto
{
    public int ListingId { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal AskingPrice { get; set; }
    public MarketplaceTripDetailsDto TripDetails { get; set; } = new();
    public string SellerName { get; set; } = string.Empty;
}

public class MarketplaceTripDetailsDto
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string OriginGov { get; set; } = string.Empty;
    public string DestinationGov { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string Class { get; set; } = string.Empty;
}
