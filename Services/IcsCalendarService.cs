using System.Text;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Services
{
    public class IcsCalendarService : IIcsCalendarService
    {
        public string GenerateIcsFile(Interview interview)
        {
            var ics = new StringBuilder();

            // Calendar header
            ics.AppendLine("BEGIN:VCALENDAR");
            ics.AppendLine("VERSION:2.0");
            ics.AppendLine("PRODID:-//ResuMate//Interview Scheduler//EN");
            ics.AppendLine("CALSCALE:GREGORIAN");
            ics.AppendLine("METHOD:REQUEST");

            // Event
            ics.AppendLine("BEGIN:VEVENT");
            ics.AppendLine($"UID:interview-{interview.Id}@resumate.com");
            ics.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmss}Z");
            ics.AppendLine($"DTSTART:{interview.ScheduledDateTime.ToUniversalTime():yyyyMMddTHHmmss}Z");

            var endTime = interview.ScheduledDateTime.AddMinutes(interview.DurationMinutes);
            ics.AppendLine($"DTEND:{endTime.ToUniversalTime():yyyyMMddTHHmmss}Z");

            // Summary (title)
            var jobTitle = interview.Application?.Job?.Title ?? "Interview";
            var companyName = interview.Application?.Job?.Company?.Name ?? "Company";
            ics.AppendLine($"SUMMARY:Interview - {jobTitle} at {companyName}");

            // Description
            var description = BuildDescription(interview);
            ics.AppendLine($"DESCRIPTION:{EscapeIcsString(description)}");

            // Location
            var location = GetLocation(interview);
            if (!string.IsNullOrEmpty(location))
            {
                ics.AppendLine($"LOCATION:{EscapeIcsString(location)}");
            }

            // Status
            ics.AppendLine("STATUS:CONFIRMED");

            // Organizer (recruiter email)
            if (!string.IsNullOrEmpty(interview.Recruiter?.Email))
            {
                ics.AppendLine($"ORGANIZER;CN={interview.Recruiter.Name}:mailto:{interview.Recruiter.Email}");
            }

            // Attendee (applicant email)
            if (!string.IsNullOrEmpty(interview.Application?.Applicant?.Email))
            {
                var applicantName = interview.Application.Applicant.FullName ?? "Applicant";
                ics.AppendLine($"ATTENDEE;CN={applicantName};RSVP=TRUE:mailto:{interview.Application.Applicant.Email}");
            }

            // Reminder (30 minutes before)
            ics.AppendLine("BEGIN:VALARM");
            ics.AppendLine("TRIGGER:-PT30M");
            ics.AppendLine("ACTION:DISPLAY");
            ics.AppendLine("DESCRIPTION:Interview Reminder");
            ics.AppendLine("END:VALARM");

            ics.AppendLine("END:VEVENT");
            ics.AppendLine("END:VCALENDAR");

            return ics.ToString();
        }

        public byte[] GenerateIcsFileBytes(Interview interview)
        {
            var icsContent = GenerateIcsFile(interview);
            return Encoding.UTF8.GetBytes(icsContent);
        }

        private string BuildDescription(Interview interview)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Interview Type: {interview.Type}");
            sb.AppendLine($"Interview Round: {interview.Round}");
            sb.AppendLine($"Duration: {interview.DurationMinutes} minutes");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(interview.MeetingLink))
            {
                sb.AppendLine($"Meeting Link: {interview.MeetingLink}");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(interview.SpecialInstructions))
            {
                sb.AppendLine("Special Instructions:");
                sb.AppendLine(interview.SpecialInstructions);
                sb.AppendLine();
            }

            if (interview.Application?.Job != null)
            {
                sb.AppendLine($"Position: {interview.Application.Job.Title}");
                if (interview.Application.Job.Company != null)
                {
                    sb.AppendLine($"Company: {interview.Application.Job.Company.Name}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private string GetLocation(Interview interview)
        {
            if (interview.Type == InterviewType.Video && !string.IsNullOrEmpty(interview.MeetingLink))
            {
                return interview.MeetingLink;
            }
            else if (interview.Type == InterviewType.InPerson && !string.IsNullOrEmpty(interview.Location))
            {
                return interview.Location;
            }
            else if (interview.Type == InterviewType.Phone)
            {
                return "Phone Interview";
            }

            return string.Empty;
        }

        private string EscapeIcsString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Escape special characters for ICS format
            return input
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n");
        }
    }
}
