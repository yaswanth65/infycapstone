using EventManagementSystemServiceLayer.Dtos;
using EventManagementSystemServiceLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/events")]
[Produces("application/json")]
public sealed class PublicEventsController : ControllerBase
{
   private readonly IPublicEventService _service;

   public PublicEventsController(IPublicEventService service)
   {
       _service = service;
   }

   [HttpGet]
   [ProducesResponseType(typeof(IReadOnlyList<PublicEventDto>), StatusCodes.Status200OK)]
   public async Task<IActionResult> Search([FromQuery] PublicEventQueryDto query, CancellationToken ct)
   {
       var results = await _service.GetPublicEventsAsync(query ?? new PublicEventQueryDto(), ct);
       return Ok(results);
   }

   [HttpGet("{eventId:long}")]
   [ProducesResponseType(typeof(PublicEventDto), StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   public async Task<IActionResult> GetById([FromRoute] long eventId, CancellationToken ct)
   {
       var result = await _service.GetEventByIdAsync(eventId, ct);
       return result is null ? NotFound() : Ok(result);
   }
}
