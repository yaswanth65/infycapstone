using System.Text;
using EventManagementSystemServiceLayer.DTOs.Brownfield;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface ICalendarExportService
    {
        string GenerateIcsContent(CalendarEventDetailsDto details);
    }

    public sealed class CalendarExportService : ICalendarExportService
    {
        public string GenerateIcsContent(CalendarEventDetailsDto d)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//EventManagementSystem//EventCalendar 1.0//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:event-{d.EventId}@eventmanagement.com");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART:{d.StartAtUtc:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTEND:{d.EndAtUtc:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"SUMMARY:{Escape(d.Title)}");
            
            var desc = d.Description ?? "";
            if (d.IsVirtual && !string.IsNullOrWhiteSpace(d.VirtualMeetingUrl))
            {
                desc += $"\nVirtual Meeting Link: {d.VirtualMeetingUrl}";
            }
            sb.AppendLine($"DESCRIPTION:{Escape(desc)}");
            sb.AppendLine($"LOCATION:{Escape(d.Venue)}");
            sb.AppendLine("STATUS:CONFIRMED");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");

            return sb.ToString();
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace(",", "\\,").Replace(";", "\\;").Replace("\n", "\\n");
        }
    }
}

