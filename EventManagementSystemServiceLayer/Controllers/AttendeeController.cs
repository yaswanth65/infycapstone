using System.Security.Claims;
using EventManagementSystemServiceLayer.Dtos;
using EventManagementSystemServiceLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers;

[ApiController]
[Authorize(Roles = "Attendee")]
[Route("api/v1/attendee")]
[Produces("application/json")]
public sealed class AttendeeController : ControllerBase
{
   private readonly IEventRegistrationService _registration;
   private readonly IAttendeeHistoryService _history;

   public AttendeeController(IEventRegistrationService registration, IAttendeeHistoryService history)
   {
       _registration = registration;
       _history = history;
   }

   [HttpPost("registrations")]
   [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status201Created)]
   [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status202Accepted)]
   [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status409Conflict)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   public async Task<IActionResult> Register([FromBody] RegisterEventDto dto, CancellationToken ct)
   {
       if (!TryGetAttendeeUserId(out var attendeeUserId))
       {
           return Unauthorized();
       }

       var result = await _registration.RegisterAsync(attendeeUserId, dto, ct);
       return result.Outcome switch
       {
           RegistrationServiceOutcome.Confirmed => StatusCode(StatusCodes.Status201Created, result.Response),
           RegistrationServiceOutcome.Waitlisted => StatusCode(StatusCodes.Status202Accepted, result.Response),
           RegistrationServiceOutcome.DuplicateRegistration => Conflict(result.Response),
           RegistrationServiceOutcome.DuplicateWaitlist => Conflict(result.Response),
           RegistrationServiceOutcome.EventNotAvailable => BadRequest(result.Response),
           _ => BadRequest(result.Response)
       };
   }

   [HttpDelete("registrations/{registrationId:long}")]
   [ProducesResponseType(typeof(CancellationResponseDto), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(CancellationResponseDto), StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   public async Task<IActionResult> Cancel([FromRoute] long registrationId, CancellationToken ct)
   {
       if (!TryGetAttendeeUserId(out var attendeeUserId))
       {
           return Unauthorized();
       }

       var result = await _registration.CancelRegistrationAsync(attendeeUserId, registrationId, ct);
       if (result.Cancelled) return Ok(result);
       if (string.Equals(result.Reason, "Registration not found.", StringComparison.OrdinalIgnoreCase))
       {
           return NotFound(result);
       }
       return BadRequest(result);
   }

   [HttpGet("my-events")]
   [ProducesResponseType(typeof(MyEventsDto), StatusCodes.Status200OK)]
   public async Task<IActionResult> GetMyEvents(CancellationToken ct)
   {
       if (!TryGetAttendeeUserId(out var attendeeUserId))
       {
           return Unauthorized();
       }

       var result = await _history.GetMyEventsAsync(attendeeUserId, ct);
       return Ok(result);
   }

   private bool TryGetAttendeeUserId(out long attendeeUserId)
   {
       var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
       return long.TryParse(raw, out attendeeUserId);
   }
}
