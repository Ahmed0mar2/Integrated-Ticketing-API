namespace GP.Application.Interfaces
{
    public interface ITripOccurrenceService
    {
        /// <summary>
        /// Generates physical occurrences and seat inventories for all active trips for the next N days.
        /// </summary>
        Task GenerateOccurrencesAsync(int targetDaysAhead = 60, CancellationToken cancellationToken = default);
    }
}
