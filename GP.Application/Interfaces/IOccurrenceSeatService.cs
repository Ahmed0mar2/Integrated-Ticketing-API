using GP.Application.DTOs.Occurrences;

namespace GP.Application.Interfaces;

public interface IOccurrenceSeatService
{
    Task<OccurrenceSeatsResponseDto?> GetOccurrenceSeatsAsync(int occurrenceId, CancellationToken cancellationToken = default);
}
