namespace GP.Application.DTOs.Marketplace;

public class MarketplaceListingResponseDto
{
    public int ListingId { get; set; }
    public int SellerId { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal AskingPrice { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public int SeatsCount { get; set; }
    public MarketplaceTripDetailsDto TripDetails { get; set; } = new();
}

public class MarketplaceTripDetailsDto
{
    public string OriginStationNameAr { get; set; } = string.Empty;
    public string OriginStationNameEn { get; set; } = string.Empty;
    public string? OriginGovAr { get; set; }
    public string? OriginGovEn { get; set; }
    public string DestinationStationNameAr { get; set; } = string.Empty;
    public string DestinationStationNameEn { get; set; } = string.Empty;
    public string? DestinationGovAr { get; set; }
    public string? DestinationGovEn { get; set; }
    public string AgencyName { get; set; } = string.Empty;
    public string? AgencyNameAr { get; set; }
    public DateTime Time { get; set; }
    public string Class { get; set; } = string.Empty;
    public string? ClassNameAr { get; set; }
}
