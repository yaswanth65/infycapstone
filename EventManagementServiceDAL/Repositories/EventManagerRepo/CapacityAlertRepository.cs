using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.EventManagerRepo
{
    public sealed class CapacityAlertRepository : ICapacityAlertRepository
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<CapacityAlertRepository> _logger;

        public CapacityAlertRepository(EventManagementDbContext db, ILogger<CapacityAlertRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<EventCapacityAlertConfig>> GetConfigsForEventAsync(long eventId, CancellationToken ct = default)
        {
            return await _db.EventCapacityAlertConfigs
                .AsNoTracking()
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.ThresholdPercentage)
                .ToListAsync(ct);
        }

        public async Task<EventCapacityAlertConfig> SetConfigAsync(EventCapacityAlertConfig config, CancellationToken ct = default)
        {
            var existing = await _db.EventCapacityAlertConfigs
                .FirstOrDefaultAsync(c => c.EventId == config.EventId && c.ThresholdPercentage == config.ThresholdPercentage, ct);

            if (existing != null)
            {
                existing.IsTriggered = false;
                existing.TriggeredAtUtc = null;
                await _db.SaveChangesAsync(ct);
                return existing;
            }

            config.CreatedAtUtc = DateTime.UtcNow;
            config.IsTriggered = false;
            _db.EventCapacityAlertConfigs.Add(config);
            await _db.SaveChangesAsync(ct);
            return config;
        }

        public async Task<IReadOnlyList<EventCapacityAlertConfig>> GetUntriggeredConfigsAsync(long eventId, CancellationToken ct = default)
        {
            return await _db.EventCapacityAlertConfigs
                .Where(c => c.EventId == eventId && !c.IsTriggered)
                .OrderBy(c => c.ThresholdPercentage)
                .ToListAsync(ct);
        }

        public async Task MarkTriggeredAsync(long alertConfigId, CancellationToken ct = default)
        {
            var cfg = await _db.EventCapacityAlertConfigs.FindAsync(new object[] { alertConfigId }, ct);
            if (cfg != null)
            {
                cfg.IsTriggered = true;
                cfg.TriggeredAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
