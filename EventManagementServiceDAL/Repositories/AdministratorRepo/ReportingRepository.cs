using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
   public class ReportingRepository : IReportingRepository
   {
       private readonly EventManagementDbContext _context;

       public ReportingRepository(EventManagementDbContext context)
       {
           _context = context ?? throw new ArgumentNullException(nameof(context));
       }

       public async Task<List<Registration>> GetRegistrationReportAsync(
           long? eventId,
           DateTime? startDate,
           DateTime? endDate,
           int pageNumber,
           int pageSize)
       {
           try
           {
               if (pageNumber < 1) pageNumber = 1;
               if (pageSize < 1) pageSize = 20;

               var query = BuildRegistrationQuery(eventId, startDate, endDate);

               return await query
                   .Include(r => r.Event)
                   .Include(r => r.AttendeeUser)
                   .OrderByDescending(r => r.RegisteredAtUtc)
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetRegistrationReportAsync: {ex.Message}");
               return new List<Registration>();
           }
       }

       public async Task<int> GetRegistrationReportCountAsync(long? eventId, DateTime? startDate, DateTime? endDate)
       {
           try
           {
               return await BuildRegistrationQuery(eventId, startDate, endDate).CountAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetRegistrationReportCountAsync: {ex.Message}");
               return 0;
           }
       }

       public async Task<List<AttendanceRecord>> GetAttendanceReportAsync(
           long? eventId,
           DateTime? startDate,
           DateTime? endDate,
           int pageNumber,
           int pageSize)
       {
           try
           {
               if (pageNumber < 1) pageNumber = 1;
               if (pageSize < 1) pageSize = 20;

               var query = BuildAttendanceQuery(eventId, startDate, endDate);

               return await query
                   .Include(a => a.Registration)
                       .ThenInclude(r => r.Event)
                   .Include(a => a.Registration)
                       .ThenInclude(r => r.AttendeeUser)
                   .OrderByDescending(a => a.RecordedAtUtc)
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAttendanceReportAsync: {ex.Message}");
               return new List<AttendanceRecord>();
           }
       }

       public async Task<int> GetAttendanceReportCountAsync(long? eventId, DateTime? startDate, DateTime? endDate)
       {
           try
           {
               return await BuildAttendanceQuery(eventId, startDate, endDate).CountAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAttendanceReportCountAsync: {ex.Message}");
               return 0;
           }
       }

       private IQueryable<Registration> BuildRegistrationQuery(long? eventId, DateTime? startDate, DateTime? endDate)
       {
           var query = _context.Registrations.AsQueryable();

           if (eventId.HasValue)
               query = query.Where(r => r.EventId == eventId);
           if (startDate.HasValue)
               query = query.Where(r => r.RegisteredAtUtc >= startDate);
           if (endDate.HasValue)
               query = query.Where(r => r.RegisteredAtUtc <= endDate);

           return query;
       }

       private IQueryable<AttendanceRecord> BuildAttendanceQuery(long? eventId, DateTime? startDate, DateTime? endDate)
       {
           var query = _context.AttendanceRecords.AsQueryable();

           if (eventId.HasValue)
               query = query.Where(a => a.Registration.EventId == eventId);
           if (startDate.HasValue)
               query = query.Where(a => a.RecordedAtUtc >= startDate);
           if (endDate.HasValue)
               query = query.Where(a => a.RecordedAtUtc <= endDate);

           return query;
       }
   }
}
