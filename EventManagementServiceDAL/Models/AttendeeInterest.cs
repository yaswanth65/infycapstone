namespace EventManagementServiceDAL.Models;

public partial class AttendeeInterest
{
    public long InterestId { get; set; }
    public long UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public DateTime UpdatedAtUtc { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual EventCategory Category { get; set; } = null!;
}
