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
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _db.Events
                .AsNoTracking()
                .Include(e => e.EventCategoryMappings)
                    .ThenInclude(m => m.Category)
                .Where(e => e.Status == PublishedStatus);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(e => e.EventCategoryMappings.Any(m => m.CategoryId == categoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(e => EF.Functions.Like(e.Title, $"%{k}%")
                    || (e.Description != null && EF.Functions.Like(e.Description, $"%{k}%"))
                    || e.EventCategoryMappings.Any(m => EF.Functions.Like(m.Category.CategoryName, $"%{k}%")));
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

            var list = await query
                .OrderBy(e => e.StartAtUtc)
                .ToListAsync(cancellationToken);

            var projected = list.Select(e => new PublicEventListItem
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
                IsVirtual = e.IsVirtual,
                CategoryIds = e.EventCategoryMappings.Select(m => m.CategoryId).ToList(),
                Categories = e.EventCategoryMappings.Select(m => m.Category.CategoryName).ToList(),
                ConfirmedCount = _db.Registrations.Count(r => r.EventId == e.EventId && r.RegistrationStatus == ConfirmedStatus),
                WaitlistCount = _db.WaitlistEntries.Count(w => w.EventId == e.EventId && w.WaitlistStatus == WaitingStatus)
            }).ToList();

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
            var e = await _db.Events
                .AsNoTracking()
                .Include(ev => ev.EventCategoryMappings)
                    .ThenInclude(m => m.Category)
                .FirstOrDefaultAsync(ev => ev.EventId == eventId && ev.Status == PublishedStatus, cancellationToken);

            if (e is null) return null;

            return new PublicEventListItem
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
                IsVirtual = e.IsVirtual,
                CategoryIds = e.EventCategoryMappings.Select(m => m.CategoryId).ToList(),
                Categories = e.EventCategoryMappings.Select(m => m.Category.CategoryName).ToList(),
                ConfirmedCount = await _db.Registrations.CountAsync(r => r.EventId == e.EventId && r.RegistrationStatus == ConfirmedStatus, cancellationToken),
                WaitlistCount = await _db.WaitlistEntries.CountAsync(w => w.EventId == e.EventId && w.WaitlistStatus == WaitingStatus, cancellationToken),
                WaitlistAttendees = await _db.WaitlistEntries
                    .Where(w => w.EventId == e.EventId && w.WaitlistStatus == WaitingStatus)
                    .OrderBy(w => w.QueuedAtUtc)
                    .Select(w => new WaitlistAttendeeItem
                    {
                        AttendeeUserId = w.AttendeeUserId,
                        DisplayName = w.AttendeeUser.DisplayName,
                        Position = 1,
                        RequestedAtUtc = w.QueuedAtUtc
                    }).ToListAsync(cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch published event {EventId}.", eventId);
            throw;
        }
    }
}
