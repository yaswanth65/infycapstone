using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.CommonRepo
{
    public sealed class VenueRepository : IVenueRepository
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<VenueRepository> _logger;

        public VenueRepository(EventManagementDbContext db, ILogger<VenueRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Venue>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default)
        {
            var query = _db.Venues.AsNoTracking();
            if (onlyActive)
            {
                query = query.Where(v => v.IsActive);
            }
            return await query.OrderBy(v => v.Name).ToListAsync(ct);
        }

        public async Task<Venue?> GetByIdAsync(long venueId, CancellationToken ct = default)
        {
            return await _db.Venues.FirstOrDefaultAsync(v => v.VenueId == venueId, ct);
        }

        public async Task<Venue?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            var trimmed = name.Trim();
            return await _db.Venues.FirstOrDefaultAsync(v => v.Name.ToLower() == trimmed.ToLower(), ct);
        }

        public async Task<Venue> AddAsync(Venue venue, CancellationToken ct = default)
        {
            venue.CreatedAtUtc = DateTime.UtcNow;
            _db.Venues.Add(venue);
            await _db.SaveChangesAsync(ct);
            return venue;
        }

        public async Task<Venue> UpdateAsync(Venue venue, CancellationToken ct = default)
        {
            _db.Venues.Update(venue);
            await _db.SaveChangesAsync(ct);
            return venue;
        }

        public async Task<bool> HasScheduleOverlapAsync(long venueId, DateTime startUtc, DateTime endUtc, long? excludeEventId = null, CancellationToken ct = default)
        {
            var query = _db.Events.AsNoTracking()
                .Where(e => e.VenueId == venueId && e.Status != "Cancelled");

            if (excludeEventId.HasValue)
            {
                query = query.Where(e => e.EventId != excludeEventId.Value);
            }

            // An overlap occurs if the requested start is before the existing event ends,
            // AND requested end is after the existing event starts.
            return await query.AnyAsync(e => startUtc < e.EndAtUtc && endUtc > e.StartAtUtc, ct);
        }
    }
}
