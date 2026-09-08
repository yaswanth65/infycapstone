using System.Security.Claims;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.EventManager;
using EventManagementSystemServiceLayer.Services.EventManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
   [ApiController]
   [Route("api/v1/event-manager")]
   [Authorize(Policy = "EventManager")]
   public class EventManagerController : ControllerBase
   {
       private readonly IEventManagementService _eventManagement;
       private readonly IAttendanceService _attendance;
       private readonly IRegistrationRequestService _registrationRequests;
       private readonly Services.Brownfield.IRecurringEventService _recurringEventService;
       private readonly Services.Brownfield.ICapacityAlertService _capacityAlertService;

       public EventManagerController(
           IEventManagementService eventManagement,
           IAttendanceService attendance,
           IRegistrationRequestService registrationRequests,
           Services.Brownfield.IRecurringEventService recurringEventService,
           Services.Brownfield.ICapacityAlertService capacityAlertService)
       {
           _eventManagement = eventManagement;
           _attendance = attendance;
           _registrationRequests = registrationRequests;
           _recurringEventService = recurringEventService;
           _capacityAlertService = capacityAlertService;
       }

       [HttpPost("events")]
       public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
       {
           try
           {
               if (dto == null) return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));
               var result = await _eventManagement.CreateAsync(GetCurrentUserId(), dto, GetClientIpAddress());
               if (result == null) return BadRequest(new ApiResponse<object>(false, 400, "Failed to create event."));
               return CreatedAtAction(nameof(GetEvent), new { eventId = result.EventId }, new ApiResponse<EventResponseDto>(true, 201, "Event created.", result));
           }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
            }
            catch (Exception ex)
           {
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while creating the event.", null));
           }
       }

       [HttpPut("events")]
       public async Task<IActionResult> UpdateEvent([FromBody] EventUpdateDto dto)
       {
           try
           {
               if (dto == null) return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));
               var result = await _eventManagement.UpdateAsync(GetCurrentUserId(), dto, GetClientIpAddress());
               if (result == null) return NotFound(new ApiResponse<object>(false, 404, $"Event with ID {dto.EventId} not found or cannot be updated."));
               return Ok(new ApiResponse<EventResponseDto>(true, 200, "Event updated.", result));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch (Exception ex)
           {
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while updating the event."));
           }
       }

       [HttpPost("events/{eventId:long}/publish")]
       public async Task<IActionResult> PublishEvent(long eventId, [FromBody] EventStatusTransitionDto? dto)
       {
           return await Transition(eventId, dto?.Remarks, (id, remarks) => _eventManagement.PublishAsync(GetCurrentUserId(), id, remarks, GetClientIpAddress()), "publish");
       }

       [HttpPost("events/{eventId:long}/close")]
       public async Task<IActionResult> CloseEvent(long eventId, [FromBody] EventStatusTransitionDto? dto)
       {
           return await Transition(eventId, dto?.Remarks, (id, remarks) => _eventManagement.CloseAsync(GetCurrentUserId(), id, remarks, GetClientIpAddress()), "close");
       }

       [HttpPost("events/{eventId:long}/cancel")]
       public async Task<IActionResult> CancelEvent(long eventId, [FromBody] EventStatusTransitionDto? dto)
       {
           return await Transition(eventId, dto?.Remarks, (id, remarks) => _eventManagement.CancelAsync(GetCurrentUserId(), id, remarks, GetClientIpAddress()), "cancel");
       }

       [HttpGet("events/{eventId:long}")]
       public async Task<IActionResult> GetEvent(long eventId)
       {
           var result = await _eventManagement.GetAsync(eventId);
           if (result == null) return NotFound(new ApiResponse<object>(false, 404, $"Event with ID {eventId} not found."));
           return Ok(new ApiResponse<EventResponseDto>(true, 200, "Event retrieved.", result));
       }

[HttpGet("events/{eventId:long}/registrations")]
        public async Task<IActionResult> GetEventRoster(long eventId)
        {
            try
            {
                var result = await _eventManagement.GetEventRosterAsync(GetCurrentUserId(), eventId);
                if (result == null) return NotFound(new ApiResponse<object>(false, 404, $"Event with ID {eventId} not found."));
                return Ok(new ApiResponse<EventRosterResponseDto>(true, 200, "Event roster retrieved.", result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
            }
            catch
            {
                return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving the event roster."));
            }
        }

        [HttpGet("events")]
        public async Task<IActionResult> ListEvents([FromQuery] string? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
       {
           var result = await _eventManagement.ListForOrganizerAsync(GetCurrentUserId(), status, pageNumber, pageSize);
           return Ok(new ApiResponse<PaginatedResponse<EventResponseDto>>(true, 200, "Events retrieved.", result));
       }

       [HttpPost("attendance")]
       public async Task<IActionResult> RecordAttendance([FromBody] AttendanceRecordDto dto)
       {
           try
           {
               if (dto == null) return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));
               var result = await _attendance.RecordAsync(GetCurrentUserId(), dto, GetClientIpAddress());
               if (result == null) return BadRequest(new ApiResponse<object>(false, 400, "Failed to record attendance."));
               return Ok(new ApiResponse<AttendanceResponseDto>(true, 200, "Attendance recorded.", result));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch
           {
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while recording attendance."));
           }
       }

       [HttpPut("attendance/{registrationId:long}/correct")]
       public async Task<IActionResult> CorrectAttendance(long registrationId, [FromBody] AttendanceCorrectionDto dto)
       {
           try
           {
               if (dto == null) return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));
               var result = await _attendance.CorrectAsync(GetCurrentUserId(), registrationId, dto, GetClientIpAddress());
               if (result == null) return NotFound(new ApiResponse<object>(false, 404, $"Attendance record not found for registration {registrationId}."));
               return Ok(new ApiResponse<AttendanceResponseDto>(true, 200, "Attendance corrected.", result));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch
           {
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while correcting attendance."));
           }
       }

       [HttpGet("attendance/registration/{registrationId:long}")]
       public async Task<IActionResult> GetAttendanceForRegistration(long registrationId)
       {
           var result = await _attendance.GetForRegistrationAsync(registrationId);
           if (result == null) return NotFound(new ApiResponse<object>(false, 404, $"Attendance record not found for registration {registrationId}."));
           return Ok(new ApiResponse<AttendanceResponseDto>(true, 200, "Attendance retrieved.", result));
       }

       [HttpGet("attendance/event/{eventId:long}")]
       public async Task<IActionResult> GetAttendanceForEvent(long eventId)
       {
           var result = await _attendance.GetForEventAsync(eventId);
           return Ok(new ApiResponse<List<AttendanceResponseDto>>(true, 200, "Attendance list retrieved.", result));
       }

       [HttpPost("registration-requests")]
       public async Task<IActionResult> CreateRegistrationRequest([FromBody] RegistrationRequestCreateDto dto)
       {
           try
           {
               if (dto == null) return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));
               var result = await _registrationRequests.CreateAsync(GetCurrentUserId(), dto, GetClientIpAddress());
               if (result == null) return BadRequest(new ApiResponse<object>(false, 400, "Failed to create registration request."));
               return CreatedAtAction(nameof(ListRegistrationRequests), new { eventId = result.EventId }, new ApiResponse<RegistrationRequestResponseDto>(true, 201, "Registration request created.", result));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch
           {
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while creating the registration request."));
           }
       }

       [HttpPost("registration-requests/{requestId:long}/decide")]
       public async Task<IActionResult> DecideRegistrationRequest(long requestId, [FromBody] RegistrationRequestDecisionDto dto)
       {
           try
           {
               if (dto == null) return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));
               var result = await _registrationRequests.DecideAsync(GetCurrentUserId(), requestId, dto, GetClientIpAddress());
               if (result == null) return NotFound(new ApiResponse<object>(false, 404, $"Registration request {requestId} not found."));
               return Ok(new ApiResponse<RegistrationRequestResponseDto>(true, 200, "Registration request decision recorded.", result));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch
           {
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while processing the registration request."));
           }
       }

       [HttpGet("registration-requests")]
       public async Task<IActionResult> ListRegistrationRequests([FromQuery] long eventId, [FromQuery] string? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
       {
           var result = await _registrationRequests.ListByEventAsync(eventId, status, pageNumber, pageSize);
           return Ok(new ApiResponse<PaginatedResponse<RegistrationRequestResponseDto>>(true, 200, "Registration requests retrieved.", result));
       }

        [HttpPost("events/recurring")]
        public async Task<IActionResult> CreateRecurringEvent([FromBody] DTOs.Brownfield.RecurringEventCreateDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _recurringEventService.CreateRecurringSeriesAsync(GetCurrentUserId(), dto, ct);
                return Ok(new ApiResponse<DTOs.Brownfield.RecurringEventSeriesDto>(true, 201, "Recurring event series created.", result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
            }
        }

        [HttpPost("events/{eventId:long}/capacity-alerts")]
        public async Task<IActionResult> ConfigureCapacityAlert(long eventId, [FromBody] DTOs.Brownfield.CapacityAlertConfigDto dto, CancellationToken ct)
        {
            if (eventId != dto.EventId)
                return BadRequest(new ApiResponse<object>(false, 400, "Event ID mismatch."));

            var result = await _capacityAlertService.SetThresholdAsync(dto, ct);
            return Ok(new ApiResponse<DTOs.Brownfield.CapacityAlertResponseDto>(true, 200, "Capacity alert threshold configured.", result));
        }

        [HttpGet("events/{eventId:long}/capacity-alerts")]
        public async Task<IActionResult> GetCapacityAlerts(long eventId, CancellationToken ct)
        {
            var list = await _capacityAlertService.GetThresholdsAsync(eventId, ct);
            return Ok(new ApiResponse<IReadOnlyList<DTOs.Brownfield.CapacityAlertResponseDto>>(true, 200, "Capacity alert thresholds retrieved.", list));
        }

       private async Task<IActionResult> Transition(long eventId, string? remarks, Func<long, string?, Task<EventResponseDto?>> action, string verb)
       {
           try
           {
               var result = await action(eventId, remarks);
               if (result == null) return NotFound(new ApiResponse<object>(false, 404, $"Event with ID {eventId} not found."));
               return Ok(new ApiResponse<EventResponseDto>(true, 200, $"Event {verb} processed.", result));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch
           {
               return StatusCode(500, new ApiResponse<object>(false, 500, $"An error occurred while {verb}ing the event."));
           }
       }

       private long GetCurrentUserId()
       {
           var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UserId");
           if (long.TryParse(claim, out var id)) return id;
           return 0;
       }

       private string GetClientIpAddress()
       {
           var x = Request.Headers["X-Forwarded-For"].FirstOrDefault();
           if (!string.IsNullOrEmpty(x)) return x.Split(',')[0].Trim();
           return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
       }
   }
}

