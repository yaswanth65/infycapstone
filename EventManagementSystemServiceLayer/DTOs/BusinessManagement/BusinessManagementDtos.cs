namespace EventManagementSystemServiceLayer.DTOs.BusinessManagement
{
    public class DashboardMetricsDto
    {
        public DateTime CapturedAtUtc { get; set; }
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalConfirmedRegistrations { get; set; }
        public double OverallCapacityUtilizationRate { get; set; }
        public double OverallAttendanceRate { get; set; }
        public List<RegistrationTrendDto> RegistrationTrends { get; set; } = new();
    }

    public class RegistrationTrendDto
    {
        public string DateLabel { get; set; } = null!;
        public int Count { get; set; }
    }

    public class EventSummaryDto
    {
        public long EventId { get; set; }
        public string Title { get; set; } = null!;
        public string Venue { get; set; } = null!;
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public string Status { get; set; } = null!;
        public int Capacity { get; set; }
        public int ConfirmedCount { get; set; }
        public int CancelledCount { get; set; }
        public double CapacityUtilizationRate { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int NoShowCount { get; set; }
        public double AttendanceRate { get; set; }
    }
}
