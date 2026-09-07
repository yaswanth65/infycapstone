using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.EventManagerRepo
{
    public interface IEventSeriesRepository
    {
        Task<EventSeries> CreateSeriesAsync(EventSeries series, IEnumerable<Event> occurrences, CancellationToken ct = default);
        Task<EventSeries?> GetSeriesByIdAsync(long seriesId, CancellationToken ct = default);
    }
}
