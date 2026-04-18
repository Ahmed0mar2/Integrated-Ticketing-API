namespace GP.Application.DTOs.Search
{
    public class IntermediateStopDto
    {
        public string StationName { get; set; } = string.Empty;
        public TimeOnly? ArrivalTime { get; set; }
        public TimeOnly? DepartureTime { get; set; }
        public int StopSequence { get; set; }
    }
}
