namespace EventManagementServiceDAL.Models;

public partial class AuditRecord
{
   public long AuditRecordId { get; set; }
   public long? ActorUserId { get; set; }
   public string ActionType { get; set; } = null!;
   public string TargetEntity { get; set; } = null!;
   public long? TargetEntityId { get; set; }
   public long? EventId { get; set; }
   public string Outcome { get; set; } = null!;
   public string? MetadataJson { get; set; }
   public DateTime CreatedAtUtc { get; set; }
   public string? IpAddress { get; set; }

   public virtual User? ActorUser { get; set; }
   public virtual Event? Event { get; set; }
}
