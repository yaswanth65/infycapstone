using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
   public interface IReportingRepository
   {
       Task<List<Registration>> GetRegistrationReportAsync(
           long? eventId,
           DateTime? startDate,
           DateTime? endDate,
           int pageNumber,
           int pageSize);
       Task<int> GetRegistrationReportCountAsync(long? eventId, DateTime? startDate, DateTime? endDate);
       Task<List<AttendanceRecord>> GetAttendanceReportAsync(
           long? eventId,
           DateTime? startDate,
           DateTime? endDate,
           int pageNumber,
           int pageSize);
       Task<int> GetAttendanceReportCountAsync(long? eventId, DateTime? startDate, DateTime? endDate);
   }
}
