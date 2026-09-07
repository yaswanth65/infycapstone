using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using EventManagementSystemServiceLayer.Services.Brownfield;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
    [ApiController]
    [Route("api/v1/venues")]
    public class VenueController : ControllerBase
    {
        private readonly IVenueService _service;

        public VenueController(IVenueService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetVenues([FromQuery] bool onlyActive = true, CancellationToken ct = default)
        {
            var list = await _service.GetVenuesAsync(onlyActive, ct);
            return Ok(new ApiResponse<IReadOnlyList<VenueResponseDto>>(true, 200, "Venues retrieved.", list));
        }

        [HttpGet("{venueId:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long venueId, CancellationToken ct = default)
        {
            var v = await _service.GetByIdAsync(venueId, ct);
            return v is null
                ? NotFound(new ApiResponse<object>(false, 404, "Venue not found."))
                : Ok(new ApiResponse<VenueResponseDto>(true, 200, "Venue retrieved.", v));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([FromBody] VenueCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var created = await _service.CreateAsync(dto, ct);
                return CreatedAtAction(nameof(GetById), new { venueId = created!.VenueId }, new ApiResponse<VenueResponseDto>(true, 201, "Venue created.", created));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
            }
        }

        [HttpPut("{venueId:long}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Update(long venueId, [FromBody] VenueUpdateDto dto, CancellationToken ct = default)
        {
            if (venueId != dto.VenueId)
                return BadRequest(new ApiResponse<object>(false, 400, "Venue ID mismatch."));

            var updated = await _service.UpdateAsync(dto, ct);
            return updated is null
                ? NotFound(new ApiResponse<object>(false, 404, "Venue not found."))
                : Ok(new ApiResponse<VenueResponseDto>(true, 200, "Venue updated.", updated));
        }

        [HttpPost("check-availability")]
        [Authorize(Roles = "Administrator,EventManager")]
        public async Task<IActionResult> CheckAvailability([FromBody] VenueAvailabilityCheckDto dto, CancellationToken ct = default)
        {
            var res = await _service.CheckAvailabilityAsync(dto, ct);
            return Ok(new ApiResponse<VenueAvailabilityResultDto>(true, 200, res.Message, res));
        }
    }
}

