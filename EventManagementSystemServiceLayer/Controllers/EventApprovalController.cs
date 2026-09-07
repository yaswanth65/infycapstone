using System.Security.Claims;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using EventManagementSystemServiceLayer.Services.Brownfield;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
    [ApiController]
    [Route("api/v1/approvals")]
    public class EventApprovalController : ControllerBase
    {
        private readonly IEventApprovalWorkflowService _service;

        public EventApprovalController(IEventApprovalWorkflowService service)
        {
            _service = service;
        }

        [HttpPost("submit")]
        [Authorize(Roles = "EventManager,Administrator")]
        public async Task<IActionResult> SubmitForApproval([FromBody] EventApprovalSubmitDto dto, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _service.SubmitForApprovalAsync(userId, dto, ct);
                return Ok(new ApiResponse<EventApprovalResponseDto>(true, 200, "Event submitted for administrative approval.", result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetPending(CancellationToken ct = default)
        {
            var list = await _service.GetPendingApprovalsAsync(ct);
            return Ok(new ApiResponse<IReadOnlyList<EventApprovalResponseDto>>(true, 200, "Pending approvals retrieved.", list));
        }

        [HttpPost("review")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ReviewApproval([FromBody] EventApprovalReviewDto dto, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var res = await _service.ReviewApprovalAsync(userId, dto, ct);
            return res is null
                ? NotFound(new ApiResponse<object>(false, 404, "Approval request not found."))
                : Ok(new ApiResponse<EventApprovalResponseDto>(true, 200, $"Event {(dto.Approve ? "approved" : "rejected")} successfully.", res));
        }

        private long GetCurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return long.TryParse(val, out var id) ? id : 0;
        }
    }
}

