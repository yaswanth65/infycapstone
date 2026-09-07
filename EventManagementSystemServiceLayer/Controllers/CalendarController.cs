using System.Security.Claims;
using System.Text;
using EventManagementServiceDAL.Repositories.AttendeeRepo;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using EventManagementSystemServiceLayer.Services.Brownfield;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
    [ApiController]
    [Route("api/v1/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarExportService _exportService;
        private readonly IEventRepository _eventRepo;
        private readonly IRegistrationRepository _registrationRepo;

        public CalendarController(
            ICalendarExportService exportService,
            IEventRepository eventRepo,
            IRegistrationRepository registrationRepo)
        {
            _exportService = exportService;
            _eventRepo = eventRepo;
            _registrationRepo = registrationRepo;
        }

        [HttpGet("events/{eventId:long}/export")]
        [Authorize(Roles = "Administrator,EventManager,BusinessManagement")]
        public async Task<IActionResult> ExportForManagers(long eventId, CancellationToken ct)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId, ct: ct);
            if (ev is null) return NotFound();

            var details = new CalendarEventDetailsDto(
                ev.EventId,
                ev.Title,
                ev.Description,
                ev.Venue,
                ev.StartAtUtc,
                ev.EndAtUtc,
                ev.IsVirtual,
                ev.VirtualMeetingUrl
            );

            var ics = _exportService.GenerateIcsContent(details);
            var bytes = Encoding.UTF8.GetBytes(ics);
            return File(bytes, "text/calendar", $"event-{eventId}.ics");
        }

        [HttpGet("registrations/{registrationId:long}/export")]
        [Authorize(Roles = "Attendee")]
        public async Task<IActionResult> ExportForAttendee(long registrationId, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var reg = await _registrationRepo.GetRegistrationAsync(registrationId, ct);
            if (reg is null || reg.AttendeeUserId != userId || reg.RegistrationStatus != "Confirmed")
            {
                return Forbid();
            }

            var ev = await _eventRepo.GetByIdAsync(reg.EventId, ct: ct);
            if (ev is null) return NotFound();

            var details = new CalendarEventDetailsDto(
                ev.EventId,
                ev.Title,
                ev.Description,
                ev.Venue,
                ev.StartAtUtc,
                ev.EndAtUtc,
                ev.IsVirtual,
                ev.VirtualMeetingUrl
            );

            var ics = _exportService.GenerateIcsContent(details);
            var bytes = Encoding.UTF8.GetBytes(ics);
            return File(bytes, "text/calendar", $"event-{ev.EventId}.ics");
        }

        private long GetCurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return long.TryParse(val, out var id) ? id : 0;
        }
    }
}

