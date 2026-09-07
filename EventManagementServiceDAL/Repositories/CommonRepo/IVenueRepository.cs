using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.CommonRepo
{
    public interface IVenueRepository
    {
        Task<IReadOnlyList<Venue>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default);
        Task<Venue?> GetByIdAsync(long venueId, CancellationToken ct = default);
        Task<Venue?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<Venue> AddAsync(Venue venue, CancellationToken ct = default);
        Task<Venue> UpdateAsync(Venue venue, CancellationToken ct = default);
        Task<bool> HasScheduleOverlapAsync(long venueId, DateTime startUtc, DateTime endUtc, long? excludeEventId = null, CancellationToken ct = default);
    }
}
