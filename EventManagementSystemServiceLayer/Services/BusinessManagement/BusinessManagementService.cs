using EventManagementServiceDAL.Models;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.BusinessManagement;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystemServiceLayer.Services.BusinessManagement
{
    public interface IBusinessManagementService
    {
        Task<DashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken ct = default);
        Task<PaginatedResponse<EventSummaryDto>> GetRegistrationSummaryReportsAsync(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default);
        Task<PaginatedResponse<EventSummaryDto>> GetAttendanceSummaryReportsAsync(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default);
        Task<PaginatedResponse<EventSummaryDto>> GetEventCompletionReportsAsync(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default);
    }

    public class BusinessManagementService : IBusinessManagementService
    {
        private readonly EventManagementDbContext _db;

        public BusinessManagementService(EventManagementDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken ct = default)
        {
            var totalEvents = await _db.Events.CountAsync(ct);
            var activeEvents = await _db.Events.CountAsync(e => e.Status == "Published", ct);
            var totalRegistrations = await _db.Registrations.CountAsync(ct);
            var totalConfirmed = await _db.Registrations.CountAsync(r => r.RegistrationStatus == "Confirmed", ct);

            var totalCapacity = await _db.Events.Where(e => e.Status == "Published").SumAsync(e => (int?)e.Capacity, ct) ?? 0;
            var overallUtilization = totalCapacity > 0 ? Math.Round(((double)totalConfirmed / totalCapacity) * 100, 2) : 0.0;

            var totalRecordedAttendance = await _db.AttendanceRecords.CountAsync(ct);
            var totalAttended = await _db.AttendanceRecords.CountAsync(a => a.AttendanceStatus == "Attended" || a.AttendanceStatus == "Present", ct);
            var overallAttendanceRate = totalRecordedAttendance > 0 ? Math.Round(((double)totalAttended / totalRecordedAttendance) * 100, 2) : 0.0;

            // Registration trends over last 7 days
            var last7Days = DateTime.UtcNow.Date.AddDays(-6);
            var rawTrends = await _db.Registrations
                .Where(r => r.RegisteredAtUtc >= last7Days)
                .GroupBy(r => r.RegisteredAtUtc.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var trends = new List<RegistrationTrendDto>();
            for (int i = 0; i < 7; i++)
            {
                var day = last7Days.AddDays(i);
                var match = rawTrends.FirstOrDefault(t => t.Date == day);
                trends.Add(new RegistrationTrendDto
                {
                    DateLabel = day.ToString("yyyy-MM-dd"),
                    Count = match?.Count ?? 0
                });
            }

            return new DashboardMetricsDto
            {
                CapturedAtUtc = DateTime.UtcNow,
                TotalEvents = totalEvents,
                ActiveEvents = activeEvents,
                TotalRegistrations = totalRegistrations,
                TotalConfirmedRegistrations = totalConfirmed,
                OverallCapacityUtilizationRate = overallUtilization,
                OverallAttendanceRate = overallAttendanceRate,
                RegistrationTrends = trends
            };
        }

        public async Task<PaginatedResponse<EventSummaryDto>> GetRegistrationSummaryReportsAsync(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
        {
            return await GetEventSummariesAsync(startDate, endDate, null, pageNumber, pageSize, ct);
        }

        public async Task<PaginatedResponse<EventSummaryDto>> GetAttendanceSummaryReportsAsync(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
        {
            return await GetEventSummariesAsync(startDate, endDate, null, pageNumber, pageSize, ct);
        }

        public async Task<PaginatedResponse<EventSummaryDto>> GetEventCompletionReportsAsync(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
        {
            return await GetEventSummariesAsync(startDate, endDate, "Closed", pageNumber, pageSize, ct);
        }

        private async Task<PaginatedResponse<EventSummaryDto>> GetEventSummariesAsync(DateTime? startDate, DateTime? endDate, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _db.Events.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(e => e.Status == statusFilter);
            if (startDate.HasValue)
                query = query.Where(e => e.StartAtUtc >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.StartAtUtc <= endDate.Value);

            var totalRecords = await query.CountAsync(ct);

            var events = await query
                .OrderByDescending(e => e.StartAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.EventId,
                    e.Title,
                    e.Venue,
                    e.StartAtUtc,
                    e.EndAtUtc,
                    e.Status,
                    e.Capacity,
                    ConfirmedCount = e.Registrations.Count(r => r.RegistrationStatus == "Confirmed"),
                    CancelledCount = e.Registrations.Count(r => r.RegistrationStatus == "Cancelled"),
                    PresentCount = _db.AttendanceRecords.Count(a => a.Registration.EventId == e.EventId && (a.AttendanceStatus == "Attended" || a.AttendanceStatus == "Present")),
                    AbsentCount = _db.AttendanceRecords.Count(a => a.Registration.EventId == e.EventId && a.AttendanceStatus == "Absent"),
                    NoShowCount = _db.AttendanceRecords.Count(a => a.Registration.EventId == e.EventId && a.AttendanceStatus == "No-show")
                })
                .ToListAsync(ct);

            var list = events.Select(e =>
            {
                var totalAttendanceRecords = e.PresentCount + e.AbsentCount + e.NoShowCount;
                return new EventSummaryDto
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    Venue = e.Venue,
                    StartAtUtc = e.StartAtUtc,
                    EndAtUtc = e.EndAtUtc,
                    Status = e.Status,
                    Capacity = e.Capacity,
                    ConfirmedCount = e.ConfirmedCount,
                    CancelledCount = e.CancelledCount,
                    CapacityUtilizationRate = e.Capacity > 0 ? Math.Round(((double)e.ConfirmedCount / e.Capacity) * 100, 2) : 0.0,
                    PresentCount = e.PresentCount,
                    AbsentCount = e.AbsentCount,
                    NoShowCount = e.NoShowCount,
                    AttendanceRate = totalAttendanceRecords > 0 ? Math.Round(((double)e.PresentCount / totalAttendanceRecords) * 100, 2) : 0.0
                };
            }).ToList();

            return new PaginatedResponse<EventSummaryDto>
            {
                Data = list,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
            };
        }
    }
}
