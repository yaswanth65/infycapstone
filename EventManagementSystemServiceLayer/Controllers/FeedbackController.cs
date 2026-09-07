using System.Security.Claims;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using EventManagementSystemServiceLayer.Services.Brownfield;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
    [ApiController]
    [Route("api/v1/feedback")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _service;

        public FeedbackController(IFeedbackService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Attendee")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var res = await _service.SubmitFeedbackAsync(userId, dto, ct);
                return Ok(new ApiResponse<FeedbackItemDto>(true, 201, "Feedback submitted successfully.", res));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
            }
        }

        [HttpGet("events/{eventId:long}/summary")]
        [Authorize(Roles = "Administrator,EventManager,BusinessManagement")]
        public async Task<IActionResult> GetFeedbackSummary(long eventId, CancellationToken ct = default)
        {
            var summary = await _service.GetEventFeedbackSummaryAsync(eventId, ct);
            return Ok(new ApiResponse<FeedbackSummaryDto>(true, 200, "Feedback summary retrieved.", summary));
        }

        private long GetCurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return long.TryParse(val, out var id) ? id : 0;
        }
    }
}

