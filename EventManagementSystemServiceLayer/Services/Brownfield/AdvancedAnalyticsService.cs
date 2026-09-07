using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public record AdvancedAnalyticsDto(
        int TotalEvents,
        int TotalRegistrations,
        int TotalAttendeesCheckedIn,
        int TotalCancellations,
        double OverallAttendanceRatePercentage,
        double OverallNoShowRatePercentage,
        double AverageSystemRating,
        List<CategoryPopularityDto> CategoryBreakdown,
        List<MonthlyTrendDto> MonthlyTrends
    );

    public record CategoryPopularityDto(string CategoryName, int EventCount, int TotalRegistrations);
    public record MonthlyTrendDto(string MonthYear, int EventsCount, int RegistrationsCount);

    public interface IAdvancedAnalyticsService
    {
        Task<AdvancedAnalyticsDto> GetSystemAnalyticsAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);
    }

    public sealed class AdvancedAnalyticsService : IAdvancedAnalyticsService
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<AdvancedAnalyticsService> _logger;

        public AdvancedAnalyticsService(EventManagementDbContext db, ILogger<AdvancedAnalyticsService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<AdvancedAnalyticsDto> GetSystemAnalyticsAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
        {
            var eventQuery = _db.Events.AsNoTracking();
            var regQuery = _db.Registrations.AsNoTracking();

            if (fromUtc.HasValue)
            {
                eventQuery = eventQuery.Where(e => e.StartAtUtc >= fromUtc.Value);
                regQuery = regQuery.Where(r => r.RegisteredAtUtc >= fromUtc.Value);
            }
            if (toUtc.HasValue)
            {
                eventQuery = eventQuery.Where(e => e.StartAtUtc <= toUtc.Value);
                regQuery = regQuery.Where(r => r.RegisteredAtUtc <= toUtc.Value);
            }

            var totalEvents = await eventQuery.CountAsync(ct);
            var totalRegistrations = await regQuery.CountAsync(ct);
            var totalCancellations = await regQuery.CountAsync(r => r.RegistrationStatus == "Cancelled", ct);
            var totalConfirmed = await regQuery.CountAsync(r => r.RegistrationStatus == "Confirmed", ct);

            var totalAttended = await _db.AttendanceRecords
                .AsNoTracking()
                .CountAsync(a => a.AttendanceStatus == "Attended", ct);

            var attendanceRate = totalConfirmed > 0 ? Math.Round((double)totalAttended / totalConfirmed * 100, 2) : 0.0;
            var noShowRate = totalConfirmed > 0 ? Math.Max(0.0, Math.Round(100.0 - attendanceRate, 2)) : 0.0;

            var avgRating = await _db.EventFeedbacks.AnyAsync(ct)
                ? Math.Round(await _db.EventFeedbacks.AverageAsync(f => (double)f.Rating, ct), 2)
                : 0.0;

            var categories = await _db.EventCategories
                .AsNoTracking()
                .Select(c => new CategoryPopularityDto(
                    c.CategoryName,
                    c.EventCategoryMappings.Count,
                    c.EventCategoryMappings.SelectMany(m => m.Event.Registrations).Count()
                ))
                .ToListAsync(ct);

            var monthlyTrends = await eventQuery
                .GroupBy(e => new { Year = e.StartAtUtc.Year, Month = e.StartAtUtc.Month })
                .Select(g => new MonthlyTrendDto(
                    g.Key.Year.ToString() + "-" + g.Key.Month.ToString("D2"),
                    g.Count(),
                    g.SelectMany(e => e.Registrations).Count()
                ))
                .OrderBy(m => m.MonthYear)
                .ToListAsync(ct);

            return new AdvancedAnalyticsDto(
                totalEvents,
                totalRegistrations,
                totalAttended,
                totalCancellations,
                attendanceRate,
                noShowRate,
                avgRating,
                categories,
                monthlyTrends
            );
        }
    }
}

