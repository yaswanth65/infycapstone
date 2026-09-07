using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AttendeeRepo;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface IFeedbackService
    {
        Task<FeedbackItemDto> SubmitFeedbackAsync(long attendeeUserId, FeedbackCreateDto dto, CancellationToken ct = default);
        Task<FeedbackSummaryDto> GetEventFeedbackSummaryAsync(long eventId, CancellationToken ct = default);
    }

    public sealed class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepo;
        private readonly IEventRepository _eventRepo;
        private readonly IRegistrationRepository _registrationRepo;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            IFeedbackRepository feedbackRepo,
            IEventRepository eventRepo,
            IRegistrationRepository registrationRepo,
            ILogger<FeedbackService> logger)
        {
            _feedbackRepo = feedbackRepo;
            _eventRepo = eventRepo;
            _registrationRepo = registrationRepo;
            _logger = logger;
        }

        public async Task<FeedbackItemDto> SubmitFeedbackAsync(long attendeeUserId, FeedbackCreateDto dto, CancellationToken ct = default)
        {
            var ev = await _eventRepo.GetByIdAsync(dto.EventId, ct: ct);
            if (ev is null) throw new InvalidOperationException("Event not found.");

            var reg = await _registrationRepo.GetByEventAndAttendeeAsync(dto.EventId, attendeeUserId, ct);
            if (reg is null || reg.RegistrationStatus != "Confirmed")
            {
                throw new InvalidOperationException("Only confirmed attendees can submit feedback.");
            }

            var existing = await _feedbackRepo.GetUserFeedbackAsync(dto.EventId, attendeeUserId, ct);
            if (existing != null)
            {
                throw new InvalidOperationException("You have already submitted feedback for this event.");
            }

            var entity = new EventFeedback
            {
                EventId = dto.EventId,
                AttendeeUserId = attendeeUserId,
                Rating = dto.Rating,
                Comments = dto.Comments?.Trim()
            };

            var created = await _feedbackRepo.AddFeedbackAsync(entity, ct);
            return new FeedbackItemDto(created.FeedbackId, created.EventId, created.AttendeeUserId, "", created.Rating, created.Comments, created.CreatedAtUtc);
        }

        public async Task<FeedbackSummaryDto> GetEventFeedbackSummaryAsync(long eventId, CancellationToken ct = default)
        {
            var (avg, total, dist) = await _feedbackRepo.GetSummaryForEventAsync(eventId, ct);
            var feedbacks = await _feedbackRepo.GetFeedbacksForEventAsync(eventId, ct);
            var recent = feedbacks.Take(10).Select(f => new FeedbackItemDto(
                f.FeedbackId,
                f.EventId,
                f.AttendeeUserId,
                f.AttendeeUser?.DisplayName ?? "Attendee",
                f.Rating,
                f.Comments,
                f.CreatedAtUtc
            )).ToList();

            return new FeedbackSummaryDto(eventId, avg, total, dist, recent);
        }
    }
}

