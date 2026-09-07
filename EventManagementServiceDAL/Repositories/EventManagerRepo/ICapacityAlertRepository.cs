using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.EventManagerRepo
{
    public interface ICapacityAlertRepository
    {
        Task<IReadOnlyList<EventCapacityAlertConfig>> GetConfigsForEventAsync(long eventId, CancellationToken ct = default);
        Task<EventCapacityAlertConfig> SetConfigAsync(EventCapacityAlertConfig config, CancellationToken ct = default);
        Task<IReadOnlyList<EventCapacityAlertConfig>> GetUntriggeredConfigsAsync(long eventId, CancellationToken ct = default);
        Task MarkTriggeredAsync(long alertConfigId, CancellationToken ct = default);
    }
}
