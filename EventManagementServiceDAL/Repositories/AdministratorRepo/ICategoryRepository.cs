using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
    public interface ICategoryRepository
    {
        Task<IReadOnlyList<EventCategory>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default);
        Task<EventCategory?> GetByIdAsync(int categoryId, CancellationToken ct = default);
        Task<EventCategory?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<EventCategory> AddAsync(EventCategory category, CancellationToken ct = default);
        Task<EventCategory> UpdateAsync(EventCategory category, CancellationToken ct = default);
        Task<IReadOnlyList<int>> GetCategoryIdsForEventAsync(long eventId, CancellationToken ct = default);
        Task AssignCategoriesToEventAsync(long eventId, IEnumerable<int> categoryIds, CancellationToken ct = default);
    }
}
