using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Administrator;
using EventManagementSystemServiceLayer.Services.Administrator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
   [ApiController]
   [Route("api/v1/admin/reports")]
   [Authorize(Roles = "Administrator")]
   public class AdminReportsController : ControllerBase
   {
       private readonly IAdminReportService _reportService;

       public AdminReportsController(IAdminReportService reportService)
       {
           _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
       }

       [HttpGet("registrations")]
       [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<RegistrationReportItemDto>>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> GetRegistrationReport(
           [FromQuery] long? eventId = null,
           [FromQuery] DateTime? startDate = null,
           [FromQuery] DateTime? endDate = null,
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 20)
       {
           try
           {
               var filter = BuildReportFilter(eventId, startDate, endDate, pageNumber, pageSize);
               var result = await _reportService.GetRegistrationReportAsync(filter);
               return Ok(new ApiResponse<PaginatedResponse<RegistrationReportItemDto>>(
                   true, 200, "Registration report retrieved successfully.", result));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetRegistrationReport: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving the registration report."));
           }
       }

       [HttpGet("attendance")]
       [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<AttendanceReportItemDto>>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> GetAttendanceReport(
           [FromQuery] long? eventId = null,
           [FromQuery] DateTime? startDate = null,
           [FromQuery] DateTime? endDate = null,
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 20)
       {
           try
           {
               var filter = BuildReportFilter(eventId, startDate, endDate, pageNumber, pageSize);
               var result = await _reportService.GetAttendanceReportAsync(filter);
               return Ok(new ApiResponse<PaginatedResponse<AttendanceReportItemDto>>(
                   true, 200, "Attendance report retrieved successfully.", result));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAttendanceReport: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving the attendance report."));
           }
       }

       [HttpGet("audit-trail")]
       [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<AuditLogResponseDto>>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> GetAuditTrailReport(
           [FromQuery] long? actorUserId = null,
           [FromQuery] string? actionType = null,
           [FromQuery] DateTime? startDate = null,
           [FromQuery] DateTime? endDate = null,
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 20)
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
               var result = await _reportService.GetAuditTrailReportAsync(filter);
               return Ok(new ApiResponse<PaginatedResponse<AuditLogResponseDto>>(
                   true, 200, "Audit trail report retrieved successfully.", result));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAuditTrailReport: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving the audit trail report."));
           }
       }

       [HttpGet("registrations/export")]
       [ProducesResponseType(StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> ExportRegistrationReport(
           [FromQuery] string format = "excel",
           [FromQuery] long? eventId = null,
           [FromQuery] DateTime? startDate = null,
           [FromQuery] DateTime? endDate = null)
       {
           try
           {
               var filter = BuildReportFilter(eventId, startDate, endDate, 1, 1);
               return await ExportAsync(
                   format,
                   () => _reportService.ExportRegistrationReportToExcelAsync(filter),
                   () => _reportService.ExportRegistrationReportToPdfAsync(filter),
                   "RegistrationReport");
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in ExportRegistrationReport: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while exporting the registration report."));
           }
       }

       [HttpGet("attendance/export")]
       [ProducesResponseType(StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> ExportAttendanceReport(
           [FromQuery] string format = "excel",
           [FromQuery] long? eventId = null,
           [FromQuery] DateTime? startDate = null,
           [FromQuery] DateTime? endDate = null)
       {
           try
           {
               var filter = BuildReportFilter(eventId, startDate, endDate, 1, 1);
               return await ExportAsync(
                   format,
                   () => _reportService.ExportAttendanceReportToExcelAsync(filter),
                   () => _reportService.ExportAttendanceReportToPdfAsync(filter),
                   "AttendanceReport");
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in ExportAttendanceReport: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while exporting the attendance report."));
           }
       }

       private static ReportFilterDto BuildReportFilter(long? eventId, DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
       {
           return new ReportFilterDto
           {
               EventId = eventId,
               StartDate = startDate,
               EndDate = endDate,
               PageNumber = pageNumber,
               PageSize = pageSize
           };
       }

       private async Task<IActionResult> ExportAsync(
           string format,
           Func<Task<byte[]>> excelFactory,
           Func<Task<byte[]>> pdfFactory,
           string baseFileName)
       {
           var normalized = (format ?? "excel").Trim().ToLowerInvariant();
           var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

           if (normalized == "pdf")
           {
               var pdfBytes = await pdfFactory();
               if (pdfBytes.Length == 0)
                   return StatusCode(500, new ApiResponse<object>(false, 500, "Failed to generate the PDF export."));
               return File(pdfBytes, "application/pdf", $"{baseFileName}_{timestamp}.pdf");
           }

           if (normalized == "excel" || normalized == "xlsx")
           {
               var excelBytes = await excelFactory();
               if (excelBytes.Length == 0)
                   return StatusCode(500, new ApiResponse<object>(false, 500, "Failed to generate the Excel export."));
               return File(excelBytes,
                   "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                   $"{baseFileName}_{timestamp}.xlsx");
           }

           return BadRequest(new ApiResponse<object>(
               false, 400, "Invalid export format. Supported formats are 'excel' and 'pdf'."));
       }
   }
}
