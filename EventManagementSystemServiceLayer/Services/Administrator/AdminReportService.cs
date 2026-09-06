using ClosedXML.Excel;
using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Administrator;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EventManagementSystemServiceLayer.Services.Administrator
{
   public interface IAdminReportService
   {
       Task<PaginatedResponse<RegistrationReportItemDto>> GetRegistrationReportAsync(ReportFilterDto filter);
       Task<PaginatedResponse<AttendanceReportItemDto>> GetAttendanceReportAsync(ReportFilterDto filter);
       Task<PaginatedResponse<AuditLogResponseDto>> GetAuditTrailReportAsync(AuditLogFilterDto filter);
       Task<byte[]> ExportRegistrationReportToExcelAsync(ReportFilterDto filter);
       Task<byte[]> ExportRegistrationReportToPdfAsync(ReportFilterDto filter);
       Task<byte[]> ExportAttendanceReportToExcelAsync(ReportFilterDto filter);
       Task<byte[]> ExportAttendanceReportToPdfAsync(ReportFilterDto filter);
   }

   public class AdminReportService : IAdminReportService
   {
       private readonly IReportingRepository _reportingRepository;
       private readonly IAuditMonitoringService _auditMonitoringService;

       private const int ExportPageSize = 10000;

       public AdminReportService(
           IReportingRepository reportingRepository,
           IAuditMonitoringService auditMonitoringService)
       {
           _reportingRepository = reportingRepository ?? throw new ArgumentNullException(nameof(reportingRepository));
           _auditMonitoringService = auditMonitoringService ?? throw new ArgumentNullException(nameof(auditMonitoringService));

           QuestPDF.Settings.License = LicenseType.Community;
       }

       public async Task<PaginatedResponse<RegistrationReportItemDto>> GetRegistrationReportAsync(ReportFilterDto filter)
       {
           try
           {
               filter ??= new ReportFilterDto();
               var (pageNumber, pageSize) = NormalizePaging(filter.PageNumber, filter.PageSize);

               var records = await _reportingRepository.GetRegistrationReportAsync(
                   filter.EventId, filter.StartDate, filter.EndDate, pageNumber, pageSize);
               var total = await _reportingRepository.GetRegistrationReportCountAsync(
                   filter.EventId, filter.StartDate, filter.EndDate);

               return new PaginatedResponse<RegistrationReportItemDto>
               {
                   Data = records.Select(MapRegistration).ToList(),
                   PageNumber = pageNumber,
                   PageSize = pageSize,
                   TotalRecords = total,
                   TotalPages = (int)Math.Ceiling((double)total / pageSize)
               };
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetRegistrationReportAsync: {ex.Message}");
               return EmptyPage<RegistrationReportItemDto>(filter);
           }
       }

       public async Task<PaginatedResponse<AttendanceReportItemDto>> GetAttendanceReportAsync(ReportFilterDto filter)
       {
           try
           {
               filter ??= new ReportFilterDto();
               var (pageNumber, pageSize) = NormalizePaging(filter.PageNumber, filter.PageSize);

               var records = await _reportingRepository.GetAttendanceReportAsync(
                   filter.EventId, filter.StartDate, filter.EndDate, pageNumber, pageSize);
               var total = await _reportingRepository.GetAttendanceReportCountAsync(
                   filter.EventId, filter.StartDate, filter.EndDate);

               return new PaginatedResponse<AttendanceReportItemDto>
               {
                   Data = records.Select(MapAttendance).ToList(),
                   PageNumber = pageNumber,
                   PageSize = pageSize,
                   TotalRecords = total,
                   TotalPages = (int)Math.Ceiling((double)total / pageSize)
               };
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAttendanceReportAsync: {ex.Message}");
               return EmptyPage<AttendanceReportItemDto>(filter);
           }
       }

       public async Task<PaginatedResponse<AuditLogResponseDto>> GetAuditTrailReportAsync(AuditLogFilterDto filter)
       {
           try
           {
               return await _auditMonitoringService.GetFilteredAuditLogsAsync(filter ?? new AuditLogFilterDto());
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAuditTrailReportAsync: {ex.Message}");
               return new PaginatedResponse<AuditLogResponseDto>
               {
                   Data = new List<AuditLogResponseDto>(),
                   PageNumber = filter?.PageNumber ?? 1,
                   PageSize = filter?.PageSize ?? 10,
                   TotalRecords = 0,
                   TotalPages = 0
               };
           }
       }

       public async Task<byte[]> ExportRegistrationReportToExcelAsync(ReportFilterDto filter)
       {
           try
           {
               var rows = await GetAllRegistrationRows(filter);

               using var workbook = new XLWorkbook();
               var ws = workbook.Worksheets.Add("Registrations");

               string[] headers = { "RegistrationId", "EventId", "Event Title", "Attendee", "Email", "Status", "Source", "Registered (UTC)", "Cancelled (UTC)", "Cancel Reason" };
               for (int c = 0; c < headers.Length; c++)
                   ws.Cell(1, c + 1).Value = headers[c];
               ws.Row(1).Style.Font.Bold = true;

               int row = 2;
               foreach (var r in rows)
               {
                   ws.Cell(row, 1).Value = r.RegistrationId;
                   ws.Cell(row, 2).Value = r.EventId;
                   ws.Cell(row, 3).Value = r.EventTitle;
                   ws.Cell(row, 4).Value = r.AttendeeName;
                   ws.Cell(row, 5).Value = r.AttendeeEmail;
                   ws.Cell(row, 6).Value = r.RegistrationStatus;
                   ws.Cell(row, 7).Value = r.Source;
                   ws.Cell(row, 8).Value = r.RegisteredAtUtc;
                   ws.Cell(row, 9).Value = r.CancelledAtUtc;
                   ws.Cell(row, 10).Value = r.CancelReason;
                   row++;
               }

               ws.Columns().AdjustToContents();

               using var stream = new MemoryStream();
               workbook.SaveAs(stream);
               return stream.ToArray();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in ExportRegistrationReportToExcelAsync: {ex.Message}");
               return Array.Empty<byte>();
           }
       }

       public async Task<byte[]> ExportRegistrationReportToPdfAsync(ReportFilterDto filter)
       {
           try
           {
               var rows = await GetAllRegistrationRows(filter);

               var document = Document.Create(container =>
               {
                   container.Page(page =>
                   {
                       page.Size(PageSizes.A4.Landscape());
                       page.Margin(20);
                       page.Header().Text("Registration Report").FontSize(16).Bold();
                       page.Content().Table(table =>
                       {
                           table.ColumnsDefinition(columns =>
                           {
                               columns.ConstantColumn(50);
                               columns.RelativeColumn();
                               columns.RelativeColumn();
                               columns.RelativeColumn();
                               columns.ConstantColumn(70);
                               columns.ConstantColumn(90);
                           });

                           table.Header(header =>
                           {
                               header.Cell().Text("Reg#").Bold();
                               header.Cell().Text("Event").Bold();
                               header.Cell().Text("Attendee").Bold();
                               header.Cell().Text("Email").Bold();
                               header.Cell().Text("Status").Bold();
                               header.Cell().Text("Registered (UTC)").Bold();
                           });

                           foreach (var r in rows)
                           {
                               table.Cell().Text(r.RegistrationId.ToString());
                               table.Cell().Text(r.EventTitle);
                               table.Cell().Text(r.AttendeeName);
                               table.Cell().Text(r.AttendeeEmail);
                               table.Cell().Text(r.RegistrationStatus);
                               table.Cell().Text(r.RegisteredAtUtc.ToString("u"));
                           }
                       });
                       page.Footer().AlignRight().Text(t =>
                       {
                           t.Span("Generated (UTC): ");
                           t.Span(DateTime.UtcNow.ToString("u"));
                       });
                   });
               });

               return document.GeneratePdf();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in ExportRegistrationReportToPdfAsync: {ex.Message}");
               return Array.Empty<byte>();
           }
       }

       public async Task<byte[]> ExportAttendanceReportToExcelAsync(ReportFilterDto filter)
       {
           try
           {
               var rows = await GetAllAttendanceRows(filter);

               using var workbook = new XLWorkbook();
               var ws = workbook.Worksheets.Add("Attendance");

               string[] headers = { "AttendanceRecordId", "RegistrationId", "EventId", "Event Title", "Attendee", "Email", "Status", "Recorded (UTC)", "Finalized", "Correction Reason" };
               for (int c = 0; c < headers.Length; c++)
                   ws.Cell(1, c + 1).Value = headers[c];
               ws.Row(1).Style.Font.Bold = true;

               int row = 2;
               foreach (var a in rows)
               {
                   ws.Cell(row, 1).Value = a.AttendanceRecordId;
                   ws.Cell(row, 2).Value = a.RegistrationId;
                   ws.Cell(row, 3).Value = a.EventId;
                   ws.Cell(row, 4).Value = a.EventTitle;
                   ws.Cell(row, 5).Value = a.AttendeeName;
                   ws.Cell(row, 6).Value = a.AttendeeEmail;
                   ws.Cell(row, 7).Value = a.AttendanceStatus;
                   ws.Cell(row, 8).Value = a.RecordedAtUtc;
                   ws.Cell(row, 9).Value = a.IsFinalized;
                   ws.Cell(row, 10).Value = a.CorrectionReason;
                   row++;
               }

               ws.Columns().AdjustToContents();

               using var stream = new MemoryStream();
               workbook.SaveAs(stream);
               return stream.ToArray();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in ExportAttendanceReportToExcelAsync: {ex.Message}");
               return Array.Empty<byte>();
           }
       }

       public async Task<byte[]> ExportAttendanceReportToPdfAsync(ReportFilterDto filter)
       {
           try
           {
               var rows = await GetAllAttendanceRows(filter);

               var document = Document.Create(container =>
               {
                   container.Page(page =>
                   {
                       page.Size(PageSizes.A4.Landscape());
                       page.Margin(20);
                       page.Header().Text("Attendance Report").FontSize(16).Bold();
                       page.Content().Table(table =>
                       {
                           table.ColumnsDefinition(columns =>
                           {
                               columns.ConstantColumn(50);
                               columns.RelativeColumn();
                               columns.RelativeColumn();
                               columns.RelativeColumn();
                               columns.ConstantColumn(70);
                               columns.ConstantColumn(90);
                           });

                           table.Header(header =>
                           {
                               header.Cell().Text("Rec#").Bold();
                               header.Cell().Text("Event").Bold();
                               header.Cell().Text("Attendee").Bold();
                               header.Cell().Text("Email").Bold();
                               header.Cell().Text("Status").Bold();
                               header.Cell().Text("Recorded (UTC)").Bold();
                           });

                           foreach (var a in rows)
                           {
                               table.Cell().Text(a.AttendanceRecordId.ToString());
                               table.Cell().Text(a.EventTitle);
                               table.Cell().Text(a.AttendeeName);
                               table.Cell().Text(a.AttendeeEmail);
                               table.Cell().Text(a.AttendanceStatus);
                               table.Cell().Text(a.RecordedAtUtc.ToString("u"));
                           }
                       });
                       page.Footer().AlignRight().Text(t =>
                       {
                           t.Span("Generated (UTC): ");
                           t.Span(DateTime.UtcNow.ToString("u"));
                       });
                   });
               });

               return document.GeneratePdf();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in ExportAttendanceReportToPdfAsync: {ex.Message}");
               return Array.Empty<byte>();
           }
       }

       private async Task<List<RegistrationReportItemDto>> GetAllRegistrationRows(ReportFilterDto filter)
       {
           filter ??= new ReportFilterDto();
           var records = await _reportingRepository.GetRegistrationReportAsync(
               filter.EventId, filter.StartDate, filter.EndDate, 1, ExportPageSize);
           return records.Select(MapRegistration).ToList();
       }

       private async Task<List<AttendanceReportItemDto>> GetAllAttendanceRows(ReportFilterDto filter)
       {
           filter ??= new ReportFilterDto();
           var records = await _reportingRepository.GetAttendanceReportAsync(
               filter.EventId, filter.StartDate, filter.EndDate, 1, ExportPageSize);
           return records.Select(MapAttendance).ToList();
       }

       private static (int pageNumber, int pageSize) NormalizePaging(int pageNumber, int pageSize)
       {
           if (pageNumber < 1) pageNumber = 1;
           if (pageSize < 1) pageSize = 20;
           if (pageSize > 200) pageSize = 200;
           return (pageNumber, pageSize);
       }

       private static PaginatedResponse<T> EmptyPage<T>(ReportFilterDto? filter) => new()
       {
           Data = new List<T>(),
           PageNumber = filter?.PageNumber ?? 1,
           PageSize = filter?.PageSize ?? 20,
           TotalRecords = 0,
           TotalPages = 0
       };

       private static RegistrationReportItemDto MapRegistration(Registration r) => new()
       {
           RegistrationId = r.RegistrationId,
           EventId = r.EventId,
           EventTitle = r.Event?.Title ?? string.Empty,
           AttendeeUserId = r.AttendeeUserId,
           AttendeeName = r.AttendeeUser?.DisplayName ?? r.AttendeeUser?.UserName ?? string.Empty,
           AttendeeEmail = r.AttendeeUser?.Email ?? string.Empty,
           RegistrationStatus = r.RegistrationStatus,
           Source = r.Source,
           RegisteredAtUtc = r.RegisteredAtUtc,
           CancelledAtUtc = r.CancelledAtUtc,
           CancelReason = r.CancelReason
       };

       private static AttendanceReportItemDto MapAttendance(AttendanceRecord a) => new()
       {
           AttendanceRecordId = a.AttendanceRecordId,
           RegistrationId = a.RegistrationId,
           EventId = a.Registration?.EventId ?? 0,
           EventTitle = a.Registration?.Event?.Title ?? string.Empty,
           AttendeeUserId = a.Registration?.AttendeeUserId ?? 0,
           AttendeeName = a.Registration?.AttendeeUser?.DisplayName ?? a.Registration?.AttendeeUser?.UserName ?? string.Empty,
           AttendeeEmail = a.Registration?.AttendeeUser?.Email ?? string.Empty,
           AttendanceStatus = a.AttendanceStatus,
           RecordedAtUtc = a.RecordedAtUtc,
           IsFinalized = a.IsFinalized,
           FinalizedAtUtc = a.FinalizedAtUtc,
           CorrectionReason = a.CorrectionReason
       };
   }
}
