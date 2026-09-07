using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface IRecurringEventService
    {
        Task<RecurringEventSeriesDto> CreateRecurringSeriesAsync(long organizerUserId, RecurringEventCreateDto dto, CancellationToken ct = default);
    }

    public sealed class RecurringEventService : IRecurringEventService
    {
        private readonly IEventSeriesRepository _seriesRepo;
        private readonly IVenueRepository _venueRepo;
        private readonly ILogger<RecurringEventService> _logger;

        public RecurringEventService(IEventSeriesRepository seriesRepo, IVenueRepository venueRepo, ILogger<RecurringEventService> logger)
        {
            _seriesRepo = seriesRepo;
            _venueRepo = venueRepo;
            _logger = logger;
        }

        public async Task<RecurringEventSeriesDto> CreateRecurringSeriesAsync(long organizerUserId, RecurringEventCreateDto dto, CancellationToken ct = default)
        {
            var occurrences = new List<Event>();
            var duration = dto.FirstEndAtUtc - dto.FirstStartAtUtc;
            var currentStart = dto.FirstStartAtUtc;

            int stepDays = dto.RecurrencePattern.ToLower() switch
            {
                "daily" => Math.Max(1, dto.RecurrenceInterval),
                "weekly" => Math.Max(1, dto.RecurrenceInterval) * 7,
                "monthly" => Math.Max(1, dto.RecurrenceInterval) * 28,
                _ => 7
            };

            while (currentStart <= dto.RecurrenceEndDateUtc && occurrences.Count < 50)
            {
                var currentEnd = currentStart + duration;

                // Check venue overlap if venue specified
                if (dto.VenueId.HasValue)
                {
                    var overlap = await _venueRepo.HasScheduleOverlapAsync(dto.VenueId.Value, currentStart, currentEnd, null, ct);
                    if (overlap)
                    {
                        throw new InvalidOperationException($"Recurring series booking failed: Venue conflict on {currentStart:yyyy-MM-dd HH:mm} UTC.");
                    }
                }

                occurrences.Add(new Event
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Venue = dto.Venue,
                    VenueId = dto.VenueId,
                    StartAtUtc = currentStart,
                    EndAtUtc = currentEnd,
                    RegistrationOpenAtUtc = currentStart.AddDays(-7),
                    RegistrationCloseAtUtc = currentStart.AddHours(-2),
                    Capacity = dto.Capacity,
                    Status = "Draft",
                    ApprovalStatus = "Draft",
                    IsVirtual = dto.IsVirtual,
                    VirtualMeetingUrl = dto.VirtualMeetingUrl,
                    CreatedAtUtc = DateTime.UtcNow
                });

                currentStart = currentStart.AddDays(stepDays);
            }

            var series = new EventSeries
            {
                OrganizerUserId = organizerUserId,
                RecurrencePattern = dto.RecurrencePattern,
                RecurrenceInterval = dto.RecurrenceInterval,
                DaysOfWeekMask = dto.DaysOfWeekMask,
                RecurrenceEndDateUtc = dto.RecurrenceEndDateUtc,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createdSeries = await _seriesRepo.CreateSeriesAsync(series, occurrences, ct);
            return new RecurringEventSeriesDto(createdSeries.SeriesId, series.RecurrencePattern, occurrences.Count, occurrences.Select(o => o.EventId).ToList());
        }
    }
}

