using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Administrator;
using System.Diagnostics;

namespace EventManagementSystemServiceLayer.Services.Administrator
{
   public interface IAuditMonitoringService
   {
       Task<PaginatedResponse<AuditLogResponseDto>> GetFilteredAuditLogsAsync(AuditLogFilterDto filter);
       Task<PerformanceMetricsDto> GetPerformanceMetricsAsync();
   }

   public class AuditMonitoringService : IAuditMonitoringService
   {
       private readonly IAuditRepository _auditRepository;
       private readonly IUserRepository _userRepository;

       public AuditMonitoringService(IAuditRepository auditRepository, IUserRepository userRepository)
       {
           _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
           _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
       }

       public async Task<PaginatedResponse<AuditLogResponseDto>> GetFilteredAuditLogsAsync(AuditLogFilterDto filter)
       {
           try
           {
               if (filter == null)
                   filter = new AuditLogFilterDto();

               var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
               var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

               var records = await _auditRepository.GetFilteredAuditRecordsAsync(
                   filter.ActorUserId,
                   filter.ActionType,
                   filter.StartDate,
                   filter.EndDate,
                   pageNumber,
                   pageSize);

               var totalCount = await _auditRepository.GetFilteredAuditRecordsCountAsync(
                   filter.ActorUserId,
                   filter.ActionType,
                   filter.StartDate,
                   filter.EndDate);

               var data = records.Select(MapToResponseDto).ToList();

               return new PaginatedResponse<AuditLogResponseDto>
               {
                   Data = data,
                   PageNumber = pageNumber,
                   PageSize = pageSize,
                   TotalRecords = (int)totalCount,
                   TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
               };
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetFilteredAuditLogsAsync: {ex.Message}");
               return new PaginatedResponse<AuditLogResponseDto>
               {
                   Data = new List<AuditLogResponseDto>(),
                   PageNumber = filter?.PageNumber ?? 1,
                   PageSize = filter?.PageSize ?? 10,
                   TotalRecords = 0,
                   TotalPages = 0
               };
           }
       }

       public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync()
       {
           try
           {
               var now = DateTime.UtcNow;

               var totalActiveUsers = await _userRepository.GetTotalUserCountAsync();
               var totalAuditRecords = await _auditRepository.GetTotalAuditRecordCountAsync();
               var auditRecordsLast24Hours = await _auditRepository.GetAuditRecordCountSinceAsync(now.AddHours(-24));

               var process = Process.GetCurrentProcess();
               var memoryUsageMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);
               var uptimeSeconds = Math.Round((now - process.StartTime.ToUniversalTime()).TotalSeconds, 0);

               return new PerformanceMetricsDto
               {
                   CapturedAtUtc = now,
                   TotalActiveUsers = totalActiveUsers,
                   TotalAuditRecords = totalAuditRecords,
                   AuditRecordsLast24Hours = auditRecordsLast24Hours,
                   MemoryUsageMb = memoryUsageMb,
                   UptimeSeconds = uptimeSeconds,
                   HealthStatus = "Healthy"
               };
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetPerformanceMetricsAsync: {ex.Message}");
               return new PerformanceMetricsDto
               {
                   CapturedAtUtc = DateTime.UtcNow,
                   HealthStatus = "Degraded"
               };
           }
       }

       private AuditLogResponseDto MapToResponseDto(AuditRecord record)
       {
           return new AuditLogResponseDto
           {
               AuditRecordId = record.AuditRecordId,
               ActorUserId = record.ActorUserId,
               ActorUserName = record.ActorUser?.DisplayName ?? record.ActorUser?.UserName,
               ActionType = record.ActionType,
               TargetEntity = record.TargetEntity,
               TargetEntityId = record.TargetEntityId,
               EventId = record.EventId,
               Outcome = record.Outcome,
               IpAddress = record.IpAddress,
               MetadataJson = record.MetadataJson,
               CreatedAtUtc = record.CreatedAtUtc
           };
       }
   }
}
