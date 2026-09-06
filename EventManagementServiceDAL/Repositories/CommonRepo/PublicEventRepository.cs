using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.CommonRepo;

public sealed class PublicEventRepository : IPublicEventRepository
{
    private const string PublishedStatus = "Published";
    private const string ConfirmedStatus = "Confirmed";
    private const string WaitingStatus = "Waiting";

    private readonly EventManagementDbContext _db;
    private readonly ILogger<PublicEventRepository> _logger;

    public PublicEventRepository(EventManagementDbContext db, ILogger<PublicEventRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PublicEventListItem>> SearchPublishedEventsAsync(
        string? keyword,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? location,
        bool? onlyAvailable,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _db.Events
                .AsNoTracking()
                .Where(e => e.Status == PublishedStatus);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(e => EF.Functions.Like(e.Title, $"%{k}%")
                    || (e.Description != null && EF.Functions.Like(e.Description, $"%{k}%")));
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(e => e.StartAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(e => e.StartAtUtc <= toUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var loc = location.Trim();
                query = query.Where(e => EF.Functions.Like(e.Venue, $"%{loc}%"));
            }

            var projected = await query
                .OrderBy(e => e.StartAtUtc)
                .Select(e => new PublicEventListItem
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    Description = e.Description,
                    Venue = e.Venue,
                    StartAtUtc = e.StartAtUtc,
                    EndAtUtc = e.EndAtUtc,
                    RegistrationOpenAtUtc = e.RegistrationOpenAtUtc,
                    RegistrationCloseAtUtc = e.RegistrationCloseAtUtc,
                    Capacity = e.Capacity,
                    Status = e.Status,
                    ConfirmedCount = e.Registrations.Count(r => r.RegistrationStatus == ConfirmedStatus),
                    WaitlistCount = e.WaitlistEntries.Count(w => w.WaitlistStatus == WaitingStatus)
                })
                .ToListAsync(cancellationToken);

            if (onlyAvailable == true)
            {
                projected = projected.Where(p => p.AvailableCapacity > 0).ToList();
            }

            return projected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search published events.");
            throw;
        }
    }

    public async Task<PublicEventListItem?> GetPublishedEventByIdAsync(long eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Events
                .AsNoTracking()
                .Where(e => e.EventId == eventId && e.Status == PublishedStatus)
                .Select(e => new PublicEventListItem
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    Description = e.Description,
                    Venue = e.Venue,
                    StartAtUtc = e.StartAtUtc,
                    EndAtUtc = e.EndAtUtc,
                    RegistrationOpenAtUtc = e.RegistrationOpenAtUtc,
                    RegistrationCloseAtUtc = e.RegistrationCloseAtUtc,
                    Capacity = e.Capacity,
                    Status = e.Status,
                    ConfirmedCount = e.Registrations.Count(r => r.RegistrationStatus == ConfirmedStatus),
                    WaitlistCount = e.WaitlistEntries.Count(w => w.WaitlistStatus == WaitingStatus),
                    WaitlistAttendees = e.WaitlistEntries
                        .Where(w => w.WaitlistStatus == WaitingStatus)
                        .OrderBy(w => w.QueuedAtUtc)
                        .Select(w => new WaitlistAttendeeItem
                        {
                            AttendeeUserId = w.AttendeeUserId,
                            DisplayName = w.AttendeeUser.DisplayName,
                            Position = 1,
                            RequestedAtUtc = w.QueuedAtUtc
                        }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch published event {EventId}.", eventId);
            throw;
        }
    }
}
