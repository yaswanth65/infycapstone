using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.EventManagerRepo
{
    public sealed class EventSeriesRepository : IEventSeriesRepository
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<EventSeriesRepository> _logger;

        public EventSeriesRepository(EventManagementDbContext db, ILogger<EventSeriesRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<EventSeries> CreateSeriesAsync(EventSeries series, IEnumerable<Event> occurrences, CancellationToken ct = default)
        {
            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                series.CreatedAtUtc = DateTime.UtcNow;
                _db.EventSeries.Add(series);
                await _db.SaveChangesAsync(ct);

                foreach (var occ in occurrences)
                {
                    occ.SeriesId = series.SeriesId;
                    occ.OrganizerUserId = series.OrganizerUserId;
                    occ.CreatedAtUtc = DateTime.UtcNow;
                    _db.Events.Add(occ);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return series;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to create event series.");
                throw;
            }
        }

        public async Task<EventSeries?> GetSeriesByIdAsync(long seriesId, CancellationToken ct = default)
        {
            return await _db.EventSeries
                .Include(s => s.Events)
                .Include(s => s.OrganizerUser)
                .FirstOrDefaultAsync(s => s.SeriesId == seriesId, ct);
        }
    }
}
