namespace GP.Application.DTOs.Occurrences;

public class OccurrenceSeatsResponseDto
{
    public int OccurrenceId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public List<OccurrenceClassSeatsDto> Classes { get; set; } = [];
}

public class OccurrenceClassSeatsDto
{
    public int CoachClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? ClassNameAr { get; set; }
    public int TotalSeats { get; set; }
    public int RemainingSeats { get; set; }
    public string? LayoutType { get; set; }
    public int DeckCount { get; set; }
    public string? SeatMapJson { get; set; }
    public int AvailableCount { get; set; }
    public int PendingCount { get; set; }
    public int BookedCount { get; set; }
    public List<OccurrenceSeatDto> Seats { get; set; } = [];
}

public class OccurrenceSeatDto
{
    public string SeatNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public int? BookingId { get; set; }
    public DateTime? HoldExpiresAt { get; set; }
}
