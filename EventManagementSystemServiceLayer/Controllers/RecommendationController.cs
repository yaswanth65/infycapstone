using System.Security.Claims;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using EventManagementSystemServiceLayer.Services.Brownfield;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
    [ApiController]
    [Route("api/v1/recommendations")]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _service;

        public RecommendationController(IRecommendationService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Attendee")]
        public async Task<IActionResult> GetPersonalizedEvents([FromQuery] int limit = 10, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var items = await _service.GetRecommendationsAsync(userId, limit, ct);
            return Ok(new ApiResponse<IReadOnlyList<RecommendedEventDto>>(true, 200, "Personalized event recommendations retrieved.", items));
        }

        [HttpGet("preferences")]
        [Authorize(Roles = "Attendee")]
        public async Task<IActionResult> GetPreferences(CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var prefs = await _service.GetPreferencesAsync(userId, ct);
            return Ok(new ApiResponse<IReadOnlyList<AttendeeCategoryPreferenceDto>>(true, 200, "Attendee preferences retrieved.", prefs));
        }

        [HttpPost("preferences")]
        [Authorize(Roles = "Attendee")]
        public async Task<IActionResult> SetPreferences([FromBody] SetPreferencesDto dto, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            await _service.SetPreferencesAsync(userId, dto, ct);
            return Ok(new ApiResponse<object>(true, 200, "Preferences updated successfully.", null));
        }

        private long GetCurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return long.TryParse(val, out var id) ? id : 0;
        }
    }
}

