using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
    public sealed class CategoryRepository : ICategoryRepository
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(EventManagementDbContext db, ILogger<CategoryRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<EventCategory>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default)
        {
            var query = _db.EventCategories.AsNoTracking();
            if (onlyActive)
            {
                query = query.Where(c => c.IsActive);
            }
            return await query.OrderBy(c => c.CategoryName).ToListAsync(ct);
        }

        public async Task<EventCategory?> GetByIdAsync(int categoryId, CancellationToken ct = default)
        {
            return await _db.EventCategories.FirstOrDefaultAsync(c => c.CategoryId == categoryId, ct);
        }

        public async Task<EventCategory?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            var trimmed = name.Trim();
            return await _db.EventCategories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == trimmed.ToLower(), ct);
        }

        public async Task<EventCategory> AddAsync(EventCategory category, CancellationToken ct = default)
        {
            category.CreatedAtUtc = DateTime.UtcNow;
            _db.EventCategories.Add(category);
            await _db.SaveChangesAsync(ct);
            return category;
        }

        public async Task<EventCategory> UpdateAsync(EventCategory category, CancellationToken ct = default)
        {
            _db.EventCategories.Update(category);
            await _db.SaveChangesAsync(ct);
            return category;
        }

        public async Task<IReadOnlyList<int>> GetCategoryIdsForEventAsync(long eventId, CancellationToken ct = default)
        {
            return await _db.EventCategoryMappings
                .Where(m => m.EventId == eventId)
                .Select(m => m.CategoryId)
                .ToListAsync(ct);
        }

        public async Task AssignCategoriesToEventAsync(long eventId, IEnumerable<int> categoryIds, CancellationToken ct = default)
        {
            var existing = await _db.EventCategoryMappings.Where(m => m.EventId == eventId).ToListAsync(ct);
            _db.EventCategoryMappings.RemoveRange(existing);

            var now = DateTime.UtcNow;
            foreach (var cid in categoryIds.Distinct())
            {
                _db.EventCategoryMappings.Add(new EventCategoryMapping
                {
                    EventId = eventId,
                    CategoryId = cid,
                    AssignedAtUtc = now
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
