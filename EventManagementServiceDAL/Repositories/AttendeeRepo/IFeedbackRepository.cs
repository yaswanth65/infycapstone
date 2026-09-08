using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AttendeeRepo
{
    public interface IFeedbackRepository
    {
        Task<EventFeedback> AddFeedbackAsync(EventFeedback feedback, CancellationToken ct = default);
        Task<EventFeedback?> GetUserFeedbackAsync(long eventId, long attendeeUserId, CancellationToken ct = default);
        Task<IReadOnlyList<EventFeedback>> GetFeedbacksForEventAsync(long eventId, CancellationToken ct = default);
        Task<IReadOnlyList<EventFeedback>> GetUserFeedbacksAsync(long attendeeUserId, CancellationToken ct = default);
        Task<(double AverageRating, int TotalCount, Dictionary<int, int> StarDistribution)> GetSummaryForEventAsync(long eventId, CancellationToken ct = default);
    }
}
