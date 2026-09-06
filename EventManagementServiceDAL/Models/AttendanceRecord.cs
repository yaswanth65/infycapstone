namespace EventManagementServiceDAL.Models;

public partial class AttendanceRecord
{
   public long AttendanceRecordId { get; set; }
   public long RegistrationId { get; set; }
   public string AttendanceStatus { get; set; } = null!;
   public long RecordedByUserId { get; set; }
   public DateTime RecordedAtUtc { get; set; }
   public bool IsFinalized { get; set; }
   public DateTime? FinalizedAtUtc { get; set; }
   public long? CorrectedByUserId { get; set; }
   public DateTime? CorrectedAtUtc { get; set; }
   public string? CorrectionReason { get; set; }
   public int RevisionNo { get; set; }

   public virtual User? CorrectedByUser { get; set; }
   public virtual User RecordedByUser { get; set; } = null!;
   public virtual Registration Registration { get; set; } = null!;
}
