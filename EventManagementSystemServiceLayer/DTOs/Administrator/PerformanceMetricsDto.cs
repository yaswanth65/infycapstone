namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class PerformanceMetricsDto
   {
       public DateTime CapturedAtUtc { get; set; }
       public int TotalActiveUsers { get; set; }
       public long TotalAuditRecords { get; set; }
       public long AuditRecordsLast24Hours { get; set; }
       public double MemoryUsageMb { get; set; }
       public double UptimeSeconds { get; set; }
       public string HealthStatus { get; set; } = "Healthy";
   }
}
