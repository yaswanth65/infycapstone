using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.BusinessManagement;
using EventManagementSystemServiceLayer.Services.BusinessManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
    [ApiController]
    [Route("api/v1/business")]
    [Authorize(Policy = "BusinessManagement")]
    public class BusinessManagementController : ControllerBase
    {
        private readonly IBusinessManagementService _service;

        public BusinessManagementController(IBusinessManagementService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<DashboardMetricsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardMetrics(CancellationToken ct)
        {
            var metrics = await _service.GetDashboardMetricsAsync(ct);
            return Ok(new ApiResponse<DashboardMetricsDto>(true, 200, "Dashboard metrics retrieved successfully.", metrics));
        }

        [HttpGet("reports/registrations")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EventSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRegistrationSummaryReports(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _service.GetRegistrationSummaryReportsAsync(startDate, endDate, pageNumber, pageSize, ct);
            return Ok(new ApiResponse<PaginatedResponse<EventSummaryDto>>(true, 200, "Registration summary reports retrieved successfully.", result));
        }

        [HttpGet("reports/attendance")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EventSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAttendanceSummaryReports(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _service.GetAttendanceSummaryReportsAsync(startDate, endDate, pageNumber, pageSize, ct);
            return Ok(new ApiResponse<PaginatedResponse<EventSummaryDto>>(true, 200, "Attendance summary reports retrieved successfully.", result));
        }

        [HttpGet("reports/completion")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EventSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEventCompletionReports(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _service.GetEventCompletionReportsAsync(startDate, endDate, pageNumber, pageSize, ct);
            return Ok(new ApiResponse<PaginatedResponse<EventSummaryDto>>(true, 200, "Event completion reports retrieved successfully.", result));
        }

        [HttpGet("analytics")]
        [ProducesResponseType(typeof(ApiResponse<Services.Brownfield.AdvancedAnalyticsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdvancedAnalytics(
            [FromServices] Services.Brownfield.IAdvancedAnalyticsService analyticsService,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null,
            CancellationToken ct = default)
        {
            var result = await analyticsService.GetSystemAnalyticsAsync(fromUtc, toUtc, ct);
            return Ok(new ApiResponse<Services.Brownfield.AdvancedAnalyticsDto>(true, 200, "Advanced analytics retrieved successfully.", result));
        }
    }
}

