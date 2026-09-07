using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AttendeeRepo
{
    public interface IRecommendationRepository
    {
        Task<IReadOnlyList<AttendeeInterest>> GetUserInterestsAsync(long userId, CancellationToken ct = default);
        Task SetUserInterestsAsync(long userId, IEnumerable<(int CategoryId, decimal Weight)> interests, CancellationToken ct = default);
        Task<IReadOnlyList<Event>> GetPersonalizedEventsAsync(long userId, int limit = 10, CancellationToken ct = default);
    }
}
