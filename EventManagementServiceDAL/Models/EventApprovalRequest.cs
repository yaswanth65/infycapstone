namespace EventManagementServiceDAL.Models;

public partial class EventApprovalRequest
{
    public long ApprovalRequestId { get; set; }
    public long EventId { get; set; }
    public long RequestedByUserId { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Remarks { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual User RequestedByUser { get; set; } = null!;
    public virtual User? ReviewedByUser { get; set; }
}
