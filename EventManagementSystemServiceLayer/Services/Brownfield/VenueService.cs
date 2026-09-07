using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface IVenueService
    {
        Task<IReadOnlyList<VenueResponseDto>> GetVenuesAsync(bool onlyActive = true, CancellationToken ct = default);
        Task<VenueResponseDto?> GetByIdAsync(long venueId, CancellationToken ct = default);
        Task<VenueResponseDto?> CreateAsync(VenueCreateDto dto, CancellationToken ct = default);
        Task<VenueResponseDto?> UpdateAsync(VenueUpdateDto dto, CancellationToken ct = default);
        Task<VenueAvailabilityResultDto> CheckAvailabilityAsync(VenueAvailabilityCheckDto dto, CancellationToken ct = default);
    }

    public sealed class VenueService : IVenueService
    {
        private readonly IVenueRepository _repo;
        private readonly ILogger<VenueService> _logger;

        public VenueService(IVenueRepository repo, ILogger<VenueService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<IReadOnlyList<VenueResponseDto>> GetVenuesAsync(bool onlyActive = true, CancellationToken ct = default)
        {
            var list = await _repo.GetAllAsync(onlyActive, ct);
            return list.Select(v => new VenueResponseDto(v.VenueId, v.Name, v.Address, v.Capacity, v.ContactDetails, v.IsActive, v.CreatedAtUtc)).ToList();
        }

        public async Task<VenueResponseDto?> GetByIdAsync(long venueId, CancellationToken ct = default)
        {
            var v = await _repo.GetByIdAsync(venueId, ct);
            return v is null ? null : new VenueResponseDto(v.VenueId, v.Name, v.Address, v.Capacity, v.ContactDetails, v.IsActive, v.CreatedAtUtc);
        }

        public async Task<VenueResponseDto?> CreateAsync(VenueCreateDto dto, CancellationToken ct = default)
        {
            var existing = await _repo.GetByNameAsync(dto.Name, ct);
            if (existing != null)
                throw new InvalidOperationException($"Venue with name '{dto.Name}' already exists.");

            var entity = new Venue
            {
                Name = dto.Name.Trim(),
                Address = dto.Address?.Trim(),
                Capacity = dto.Capacity,
                ContactDetails = dto.ContactDetails?.Trim(),
                IsActive = true
            };

            var created = await _repo.AddAsync(entity, ct);
            return new VenueResponseDto(created.VenueId, created.Name, created.Address, created.Capacity, created.ContactDetails, created.IsActive, created.CreatedAtUtc);
        }

        public async Task<VenueResponseDto?> UpdateAsync(VenueUpdateDto dto, CancellationToken ct = default)
        {
            var v = await _repo.GetByIdAsync(dto.VenueId, ct);
            if (v is null) return null;

            v.Name = dto.Name.Trim();
            v.Address = dto.Address?.Trim();
            v.Capacity = dto.Capacity;
            v.ContactDetails = dto.ContactDetails?.Trim();
            v.IsActive = dto.IsActive;

            var updated = await _repo.UpdateAsync(v, ct);
            return new VenueResponseDto(updated.VenueId, updated.Name, updated.Address, updated.Capacity, updated.ContactDetails, updated.IsActive, updated.CreatedAtUtc);
        }

        public async Task<VenueAvailabilityResultDto> CheckAvailabilityAsync(VenueAvailabilityCheckDto dto, CancellationToken ct = default)
        {
            var venue = await _repo.GetByIdAsync(dto.VenueId, ct);
            if (venue is null)
                return new VenueAvailabilityResultDto(dto.VenueId, false, "Venue not found.");

            if (!venue.IsActive)
                return new VenueAvailabilityResultDto(dto.VenueId, false, "Venue is inactive.");

            var hasOverlap = await _repo.HasScheduleOverlapAsync(dto.VenueId, dto.StartAtUtc, dto.EndAtUtc, dto.ExcludeEventId, ct);
            if (hasOverlap)
            {
                return new VenueAvailabilityResultDto(dto.VenueId, false, "Selected time window conflicts with another booked event at this venue.");
            }

            return new VenueAvailabilityResultDto(dto.VenueId, true, "Venue is available for booking.");
        }
    }
}

