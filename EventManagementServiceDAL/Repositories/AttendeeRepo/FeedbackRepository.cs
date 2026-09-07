using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.AttendeeRepo
{
    public sealed class FeedbackRepository : IFeedbackRepository
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<FeedbackRepository> _logger;

        public FeedbackRepository(EventManagementDbContext db, ILogger<FeedbackRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<EventFeedback> AddFeedbackAsync(EventFeedback feedback, CancellationToken ct = default)
        {
            feedback.CreatedAtUtc = DateTime.UtcNow;
            _db.EventFeedbacks.Add(feedback);
            await _db.SaveChangesAsync(ct);
            return feedback;
        }

        public async Task<EventFeedback?> GetUserFeedbackAsync(long eventId, long attendeeUserId, CancellationToken ct = default)
        {
            return await _db.EventFeedbacks
                .FirstOrDefaultAsync(f => f.EventId == eventId && f.AttendeeUserId == attendeeUserId, ct);
        }

        public async Task<IReadOnlyList<EventFeedback>> GetFeedbacksForEventAsync(long eventId, CancellationToken ct = default)
        {
            return await _db.EventFeedbacks
                .AsNoTracking()
                .Include(f => f.AttendeeUser)
                .Where(f => f.EventId == eventId && !f.IsFlagged)
                .OrderByDescending(f => f.CreatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<(double AverageRating, int TotalCount, Dictionary<int, int> StarDistribution)> GetSummaryForEventAsync(long eventId, CancellationToken ct = default)
        {
            var feedbacks = await _db.EventFeedbacks
                .Where(f => f.EventId == eventId && !f.IsFlagged)
                .ToListAsync(ct);

            if (feedbacks.Count == 0)
            {
                return (0.0, 0, new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } });
            }

            var avg = feedbacks.Average(f => f.Rating);
            var dist = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
            foreach (var f in feedbacks)
            {
                if (dist.ContainsKey(f.Rating))
                    dist[f.Rating]++;
            }

            return (Math.Round(avg, 2), feedbacks.Count, dist);
        }
    }
}
