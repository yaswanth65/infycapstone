namespace EventManagementServiceDAL.Models;

public partial class EventStatusHistory
{
   public long EventStatusHistoryId { get; set; }
   public long EventId { get; set; }
   public string? FromStatus { get; set; }
   public string ToStatus { get; set; } = null!;
   public long ChangedByUserId { get; set; }
   public DateTime ChangedAtUtc { get; set; }
   public string? Remarks { get; set; }

   public virtual User ChangedByUser { get; set; } = null!;
   public virtual Event Event { get; set; } = null!;
}
