using EventManagementServiceDAL.Repositories.AttendeeRepo;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface IRecommendationService
    {
        Task SetPreferencesAsync(long userId, SetPreferencesDto dto, CancellationToken ct = default);
        Task<IReadOnlyList<AttendeeCategoryPreferenceDto>> GetPreferencesAsync(long userId, CancellationToken ct = default);
        Task<IReadOnlyList<RecommendedEventDto>> GetRecommendationsAsync(long userId, int limit = 10, CancellationToken ct = default);
    }

    public sealed class RecommendationService : IRecommendationService
    {
        private readonly IRecommendationRepository _repo;
        private readonly IRegistrationRepository _regRepo;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IRecommendationRepository repo,
            IRegistrationRepository regRepo,
            ILogger<RecommendationService> logger)
        {
            _repo = repo;
            _regRepo = regRepo;
            _logger = logger;
        }

        public async Task SetPreferencesAsync(long userId, SetPreferencesDto dto, CancellationToken ct = default)
        {
            var pairs = dto.Preferences.Select(p => (p.CategoryId, p.Weight));
            await _repo.SetUserInterestsAsync(userId, pairs, ct);
        }

        public async Task<IReadOnlyList<AttendeeCategoryPreferenceDto>> GetPreferencesAsync(long userId, CancellationToken ct = default)
        {
            var list = await _repo.GetUserInterestsAsync(userId, ct);
            return list.Select(i => new AttendeeCategoryPreferenceDto(i.CategoryId, i.Weight)).ToList();
        }

        public async Task<IReadOnlyList<RecommendedEventDto>> GetRecommendationsAsync(long userId, int limit = 10, CancellationToken ct = default)
        {
            var events = await _repo.GetPersonalizedEventsAsync(userId, limit, ct);
            var results = new List<RecommendedEventDto>();

            foreach (var e in events)
            {
                var confirmed = await _regRepo.CountConfirmedByEventAsync(e.EventId, ct);
                var available = Math.Max(0, e.Capacity - confirmed);
                var categories = e.EventCategoryMappings.Select(m => m.Category?.CategoryName ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList();

                results.Add(new RecommendedEventDto(
                    e.EventId,
                    e.Title,
                    e.Description,
                    e.Venue,
                    e.StartAtUtc,
                    e.EndAtUtc,
                    e.Capacity,
                    available,
                    categories,
                    e.IsVirtual
                ));
            }

            return results;
        }
    }
}

