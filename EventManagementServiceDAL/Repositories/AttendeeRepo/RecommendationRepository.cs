using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.AttendeeRepo
{
    public sealed class RecommendationRepository : IRecommendationRepository
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<RecommendationRepository> _logger;

        public RecommendationRepository(EventManagementDbContext db, ILogger<RecommendationRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AttendeeInterest>> GetUserInterestsAsync(long userId, CancellationToken ct = default)
        {
            return await _db.AttendeeInterests
                .AsNoTracking()
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .ToListAsync(ct);
        }

        public async Task SetUserInterestsAsync(long userId, IEnumerable<(int CategoryId, decimal Weight)> interests, CancellationToken ct = default)
        {
            var existing = await _db.AttendeeInterests.Where(i => i.UserId == userId).ToListAsync(ct);
            _db.AttendeeInterests.RemoveRange(existing);

            var now = DateTime.UtcNow;
            foreach (var item in interests)
            {
                _db.AttendeeInterests.Add(new AttendeeInterest
                {
                    UserId = userId,
                    CategoryId = item.CategoryId,
                    Weight = item.Weight,
                    UpdatedAtUtc = now
                });
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Event>> GetPersonalizedEventsAsync(long userId, int limit = 10, CancellationToken ct = default)
        {
            var userCategoryIds = await _db.AttendeeInterests
                .Where(i => i.UserId == userId)
                .Select(i => i.CategoryId)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            var publishedEvents = _db.Events
                .AsNoTracking()
                .Include(e => e.EventCategoryMappings)
                    .ThenInclude(m => m.Category)
                .Include(e => e.VenueNavigation)
                .Where(e => e.Status == "Published" && e.StartAtUtc >= now);

            if (userCategoryIds.Count > 0)
            {
                publishedEvents = publishedEvents.OrderByDescending(e => e.EventCategoryMappings.Count(m => userCategoryIds.Contains(m.CategoryId)))
                                                 .ThenBy(e => e.StartAtUtc);
            }
            else
            {
                publishedEvents = publishedEvents.OrderBy(e => e.StartAtUtc);
            }

            return await publishedEvents.Take(limit).ToListAsync(ct);
        }
    }
}
