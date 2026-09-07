using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryResponseDto>> GetCategoriesAsync(bool onlyActive = true, CancellationToken ct = default);
        Task<CategoryResponseDto?> GetByIdAsync(int categoryId, CancellationToken ct = default);
        Task<CategoryResponseDto?> CreateAsync(long adminUserId, CategoryCreateDto dto, CancellationToken ct = default);
        Task<CategoryResponseDto?> UpdateAsync(CategoryUpdateDto dto, CancellationToken ct = default);
    }

    public sealed class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepository repo, ILogger<CategoryService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CategoryResponseDto>> GetCategoriesAsync(bool onlyActive = true, CancellationToken ct = default)
        {
            var list = await _repo.GetAllAsync(onlyActive, ct);
            return list.Select(c => new CategoryResponseDto(c.CategoryId, c.CategoryName, c.Description, c.IsActive, c.CreatedAtUtc)).ToList();
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int categoryId, CancellationToken ct = default)
        {
            var c = await _repo.GetByIdAsync(categoryId, ct);
            return c is null ? null : new CategoryResponseDto(c.CategoryId, c.CategoryName, c.Description, c.IsActive, c.CreatedAtUtc);
        }

        public async Task<CategoryResponseDto?> CreateAsync(long adminUserId, CategoryCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var existing = await _repo.GetByNameAsync(dto.CategoryName, ct);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Category '{dto.CategoryName}' already exists.");
                }

                var entity = new EventCategory
                {
                    CategoryName = dto.CategoryName.Trim(),
                    Description = dto.Description?.Trim(),
                    IsActive = true,
                    CreatedByUserId = adminUserId
                };

                var created = await _repo.AddAsync(entity, ct);
                return new CategoryResponseDto(created.CategoryId, created.CategoryName, created.Description, created.IsActive, created.CreatedAtUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create category failed.");
                throw;
            }
        }

        public async Task<CategoryResponseDto?> UpdateAsync(CategoryUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _repo.GetByIdAsync(dto.CategoryId, ct);
            if (entity is null) return null;

            entity.CategoryName = dto.CategoryName.Trim();
            entity.Description = dto.Description?.Trim();
            entity.IsActive = dto.IsActive;

            var updated = await _repo.UpdateAsync(entity, ct);
            return new CategoryResponseDto(updated.CategoryId, updated.CategoryName, updated.Description, updated.IsActive, updated.CreatedAtUtc);
        }
    }
}

