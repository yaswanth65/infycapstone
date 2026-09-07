namespace EventManagementServiceDAL.Models;

public partial class EventCapacityAlertConfig
{
    public long AlertConfigId { get; set; }
    public long EventId { get; set; }
    public int ThresholdPercentage { get; set; }
    public bool IsTriggered { get; set; }
    public DateTime? TriggeredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public virtual Event Event { get; set; } = null!;
}
