using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface ICapacityAlertService
    {
        Task<CapacityAlertResponseDto> SetThresholdAsync(CapacityAlertConfigDto dto, CancellationToken ct = default);
        Task<IReadOnlyList<CapacityAlertResponseDto>> GetThresholdsAsync(long eventId, CancellationToken ct = default);
        Task CheckAndTriggerAlertsAsync(long eventId, int currentConfirmedCount, int totalCapacity, CancellationToken ct = default);
    }

    public sealed class CapacityAlertService : ICapacityAlertService
    {
        private readonly ICapacityAlertRepository _alertRepo;
        private readonly IEventRepository _eventRepo;
        private readonly INotificationRepository _notifications;
        private readonly ILogger<CapacityAlertService> _logger;

        public CapacityAlertService(
            ICapacityAlertRepository alertRepo,
            IEventRepository eventRepo,
            INotificationRepository notifications,
            ILogger<CapacityAlertService> logger)
        {
            _alertRepo = alertRepo;
            _eventRepo = eventRepo;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<CapacityAlertResponseDto> SetThresholdAsync(CapacityAlertConfigDto dto, CancellationToken ct = default)
        {
            var config = new EventCapacityAlertConfig
            {
                EventId = dto.EventId,
                ThresholdPercentage = dto.ThresholdPercentage
            };
            var saved = await _alertRepo.SetConfigAsync(config, ct);
            return new CapacityAlertResponseDto(saved.AlertConfigId, saved.EventId, saved.ThresholdPercentage, saved.IsTriggered, saved.TriggeredAtUtc, saved.CreatedAtUtc);
        }

        public async Task<IReadOnlyList<CapacityAlertResponseDto>> GetThresholdsAsync(long eventId, CancellationToken ct = default)
        {
            var list = await _alertRepo.GetConfigsForEventAsync(eventId, ct);
            return list.Select(c => new CapacityAlertResponseDto(c.AlertConfigId, c.EventId, c.ThresholdPercentage, c.IsTriggered, c.TriggeredAtUtc, c.CreatedAtUtc)).ToList();
        }

        public async Task CheckAndTriggerAlertsAsync(long eventId, int currentConfirmedCount, int totalCapacity, CancellationToken ct = default)
        {
            if (totalCapacity <= 0) return;

            var currentPercentage = (int)Math.Floor((double)currentConfirmedCount / totalCapacity * 100);
            var untriggered = await _alertRepo.GetUntriggeredConfigsAsync(eventId, ct);

            var ev = await _eventRepo.GetByIdAsync(eventId, ct: ct);
            if (ev is null) return;

            foreach (var cfg in untriggered)
            {
                if (currentPercentage >= cfg.ThresholdPercentage)
                {
                    await _alertRepo.MarkTriggeredAsync(cfg.AlertConfigId, ct);

                    await _notifications.CreateAsync(new Notification
                    {
                        RecipientUserId = ev.OrganizerUserId,
                        RelatedEventId = ev.EventId,
                        NotificationType = "CapacityAlert",
                        Title = $"Capacity Alert: {cfg.ThresholdPercentage}% Reached",
                        Message = $"Event '{ev.Title}' has reached {cfg.ThresholdPercentage}% capacity ({currentConfirmedCount}/{totalCapacity} seats confirmed).",
                        DeliveryStatus = "Sent",
                        SentAtUtc = DateTime.UtcNow,
                        CreatedAtUtc = DateTime.UtcNow
                    }, ct);
                }
            }
        }
    }
}

