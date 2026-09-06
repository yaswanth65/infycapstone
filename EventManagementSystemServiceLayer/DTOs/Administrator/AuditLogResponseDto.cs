namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class AuditLogResponseDto
   {
       public long AuditRecordId { get; set; }
       public long? ActorUserId { get; set; }
       public string? ActorUserName { get; set; }
       public string ActionType { get; set; } = null!;
       public string TargetEntity { get; set; } = null!;
       public long? TargetEntityId { get; set; }
       public long? EventId { get; set; }
       public string Outcome { get; set; } = null!;
       public string? IpAddress { get; set; }
       public string? MetadataJson { get; set; }
       public DateTime CreatedAtUtc { get; set; }
   }
}
