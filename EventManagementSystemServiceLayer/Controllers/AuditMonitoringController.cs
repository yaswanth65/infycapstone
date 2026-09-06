using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Administrator;
using EventManagementSystemServiceLayer.Services.Administrator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
   [ApiController]
   [Route("api/v1/admin")]
   [Authorize(Roles = "Administrator")]
   public class AuditMonitoringController : ControllerBase
   {
       private readonly IAuditMonitoringService _auditMonitoringService;

       public AuditMonitoringController(IAuditMonitoringService auditMonitoringService)
       {
           _auditMonitoringService = auditMonitoringService ?? throw new ArgumentNullException(nameof(auditMonitoringService));
       }

       [HttpGet("audit-logs")]
       [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<AuditLogResponseDto>>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> GetAuditLogs(
           [FromQuery] long? actorUserId = null,
           [FromQuery] string? actionType = null,
           [FromQuery] DateTime? startDate = null,
           [FromQuery] DateTime? endDate = null,
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 10)
       {
           try
           {
               var filter = new AuditLogFilterDto
               {
                   ActorUserId = actorUserId,
                   ActionType = actionType,
                   StartDate = startDate,
                   EndDate = endDate,
                   PageNumber = pageNumber,
                   PageSize = pageSize
               };

               var result = await _auditMonitoringService.GetFilteredAuditLogsAsync(filter);

               return Ok(new ApiResponse<PaginatedResponse<AuditLogResponseDto>>(
                   true, 200, "Audit logs retrieved successfully.", result));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAuditLogs: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving audit logs."));
           }
       }

       [HttpGet("metrics")]
       [ProducesResponseType(typeof(ApiResponse<PerformanceMetricsDto>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> GetPerformanceMetrics()
       {
           try
           {
               var metrics = await _auditMonitoringService.GetPerformanceMetricsAsync();

               return Ok(new ApiResponse<PerformanceMetricsDto>(
                   true, 200, "Performance metrics retrieved successfully.", metrics));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetPerformanceMetrics: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving performance metrics."));
           }
       }
   }
}
