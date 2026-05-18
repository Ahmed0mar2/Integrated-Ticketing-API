namespace GP.Application.DTOs.Search
{
    public class IntermediateStopDto
    {
        public string StationName { get; set; } = string.Empty;
        public string? ArabicName { get; set; }
        public string? GovernorateAr { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public TimeOnly? DepartureTime { get; set; }
        public int StopSequence { get; set; }
    }
}
