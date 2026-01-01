using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly AppDbContext _context;
        private readonly IIcsCalendarService _icsCalendarService;

        public EmailService(
            ILogger<EmailService> logger,
            AppDbContext context,
            IIcsCalendarService icsCalendarService)
        {
            _logger = logger;
            _context = context;
            _icsCalendarService = icsCalendarService;
        }

        public async Task SendInterviewInvitationAsync(Interview interview)
        {
            // Load related data
            await LoadInterviewDataAsync(interview);

            _logger.LogInformation("===== EMAIL NOTIFICATION (Interview Invitation) =====");
            _logger.LogInformation($"To: {interview.Application?.Applicant?.Email}");
            _logger.LogInformation($"Subject: Interview Invitation - {interview.Application?.Job?.Title} at {interview.Application?.Job?.Company?.Name}");
            _logger.LogInformation($"");
            _logger.LogInformation($"Dear {interview.Application?.Applicant?.FullName},");
            _logger.LogInformation($"");
            _logger.LogInformation($"Congratulations! You have been selected for an interview for the position of {interview.Application?.Job?.Title}.");
            _logger.LogInformation($"");
            _logger.LogInformation($"Interview Details:");
            _logger.LogInformation($"  Type: {interview.Type}");
            _logger.LogInformation($"  Round: {interview.Round}");
            _logger.LogInformation($"  Date: {interview.ScheduledDateTime:dddd, MMMM dd, yyyy}");
            _logger.LogInformation($"  Time: {interview.ScheduledDateTime:hh:mm tt}");
            _logger.LogInformation($"  Duration: {interview.DurationMinutes} minutes");

            if (interview.Type == InterviewType.Video && !string.IsNullOrEmpty(interview.MeetingLink))
            {
                _logger.LogInformation($"  Meeting Link: {interview.MeetingLink}");
            }
            else if (interview.Type == InterviewType.InPerson && !string.IsNullOrEmpty(interview.Location))
            {
                _logger.LogInformation($"  Location: {interview.Location}");
            }

            if (!string.IsNullOrEmpty(interview.SpecialInstructions))
            {
                _logger.LogInformation($"");
                _logger.LogInformation($"Special Instructions:");
                _logger.LogInformation($"  {interview.SpecialInstructions}");
            }

            _logger.LogInformation($"");
            _logger.LogInformation($"A calendar invitation (.ics file) is attached to this email.");
            _logger.LogInformation($"");
            _logger.LogInformation($"Calendar file content:");
            _logger.LogInformation($"{_icsCalendarService.GenerateIcsFile(interview)}");
            _logger.LogInformation($"");
            _logger.LogInformation($"Best regards,");
            _logger.LogInformation($"{interview.Application?.Job?.Company?.Name} Recruitment Team");
            _logger.LogInformation("=======================================================");

            await Task.CompletedTask;
        }

        public async Task SendApplicationReceivedAsync(Application application)
        {
            await LoadApplicationDataAsync(application);

            _logger.LogInformation("===== EMAIL NOTIFICATION (Application Received) =====");
            _logger.LogInformation($"To: {application.Applicant?.Email}");
            _logger.LogInformation($"Subject: Application Received - {application.Job?.Title}");
            _logger.LogInformation($"");
            _logger.LogInformation($"Dear {application.Applicant?.FullName},");
            _logger.LogInformation($"");
            _logger.LogInformation($"Thank you for applying for the position of {application.Job?.Title} at {application.Job?.Company?.Name}.");
            _logger.LogInformation($"We have received your application and our team will review it shortly.");
            _logger.LogInformation($"");
            _logger.LogInformation($"You will hear from us soon regarding the next steps.");
            _logger.LogInformation($"");
            _logger.LogInformation($"Best regards,");
            _logger.LogInformation($"{application.Job?.Company?.Name} Recruitment Team");
            _logger.LogInformation("====================================================");

            await Task.CompletedTask;
        }

        public async Task SendApplicationStatusChangeAsync(Application application, string newStatus)
        {
            await LoadApplicationDataAsync(application);

            _logger.LogInformation("===== EMAIL NOTIFICATION (Status Change) =====");
            _logger.LogInformation($"To: {application.Applicant?.Email}");
            _logger.LogInformation($"Subject: Application Status Update - {application.Job?.Title}");
            _logger.LogInformation($"");
            _logger.LogInformation($"Dear {application.Applicant?.FullName},");
            _logger.LogInformation($"");
            _logger.LogInformation($"Your application status for the position of {application.Job?.Title} has been updated to: {newStatus}");
            _logger.LogInformation($"");

            if (newStatus == "Rejected" && !string.IsNullOrEmpty(application.RejectionReason))
            {
                _logger.LogInformation($"Feedback: {application.RejectionReason}");
                _logger.LogInformation($"");
            }

            _logger.LogInformation($"Best regards,");
            _logger.LogInformation($"{application.Job?.Company?.Name} Recruitment Team");
            _logger.LogInformation("==============================================");

            await Task.CompletedTask;
        }

        public async Task SendInterviewReminderAsync(Interview interview)
        {
            await LoadInterviewDataAsync(interview);

            _logger.LogInformation("===== EMAIL NOTIFICATION (Interview Reminder) =====");
            _logger.LogInformation($"To: {interview.Application?.Applicant?.Email}");
            _logger.LogInformation($"Subject: Interview Reminder - {interview.Application?.Job?.Title}");
            _logger.LogInformation($"");
            _logger.LogInformation($"Dear {interview.Application?.Applicant?.FullName},");
            _logger.LogInformation($"");
            _logger.LogInformation($"This is a friendly reminder about your upcoming interview:");
            _logger.LogInformation($"");
            _logger.LogInformation($"  Position: {interview.Application?.Job?.Title}");
            _logger.LogInformation($"  Date: {interview.ScheduledDateTime:dddd, MMMM dd, yyyy}");
            _logger.LogInformation($"  Time: {interview.ScheduledDateTime:hh:mm tt}");
            _logger.LogInformation($"  Duration: {interview.DurationMinutes} minutes");

            if (interview.Type == InterviewType.Video && !string.IsNullOrEmpty(interview.MeetingLink))
            {
                _logger.LogInformation($"  Meeting Link: {interview.MeetingLink}");
            }
            else if (interview.Type == InterviewType.InPerson && !string.IsNullOrEmpty(interview.Location))
            {
                _logger.LogInformation($"  Location: {interview.Location}");
            }

            _logger.LogInformation($"");
            _logger.LogInformation($"We look forward to speaking with you!");
            _logger.LogInformation($"");
            _logger.LogInformation($"Best regards,");
            _logger.LogInformation($"{interview.Application?.Job?.Company?.Name} Recruitment Team");
            _logger.LogInformation("==================================================");

            await Task.CompletedTask;
        }

        public async Task SendInterviewCancellationAsync(Interview interview)
        {
            await LoadInterviewDataAsync(interview);

            _logger.LogInformation("===== EMAIL NOTIFICATION (Interview Cancelled) =====");
            _logger.LogInformation($"To: {interview.Application?.Applicant?.Email}");
            _logger.LogInformation($"Subject: Interview Cancelled - {interview.Application?.Job?.Title}");
            _logger.LogInformation($"");
            _logger.LogInformation($"Dear {interview.Application?.Applicant?.FullName},");
            _logger.LogInformation($"");
            _logger.LogInformation($"We regret to inform you that your interview scheduled for {interview.ScheduledDateTime:MMMM dd, yyyy at hh:mm tt} has been cancelled.");
            _logger.LogInformation($"");

            if (!string.IsNullOrEmpty(interview.CancellationReason))
            {
                _logger.LogInformation($"Reason: {interview.CancellationReason}");
                _logger.LogInformation($"");
            }

            _logger.LogInformation($"We apologize for any inconvenience this may cause.");
            _logger.LogInformation($"");
            _logger.LogInformation($"Best regards,");
            _logger.LogInformation($"{interview.Application?.Job?.Company?.Name} Recruitment Team");
            _logger.LogInformation("===================================================");

            await Task.CompletedTask;
        }

        public async Task SendInterviewRescheduleAsync(Interview interview)
        {
            await LoadInterviewDataAsync(interview);

            _logger.LogInformation("===== EMAIL NOTIFICATION (Interview Rescheduled) =====");
            _logger.LogInformation($"To: {interview.Application?.Applicant?.Email}");
            _logger.LogInformation($"Subject: Interview Rescheduled - {interview.Application?.Job?.Title}");
            _logger.LogInformation($"");
            _logger.LogInformation($"Dear {interview.Application?.Applicant?.FullName},");
            _logger.LogInformation($"");
            _logger.LogInformation($"Your interview has been rescheduled to:");
            _logger.LogInformation($"");
            _logger.LogInformation($"  New Date: {interview.ScheduledDateTime:dddd, MMMM dd, yyyy}");
            _logger.LogInformation($"  New Time: {interview.ScheduledDateTime:hh:mm tt}");
            _logger.LogInformation($"  Duration: {interview.DurationMinutes} minutes");

            if (interview.Type == InterviewType.Video && !string.IsNullOrEmpty(interview.MeetingLink))
            {
                _logger.LogInformation($"  Meeting Link: {interview.MeetingLink}");
            }

            _logger.LogInformation($"");
            _logger.LogInformation($"A new calendar invitation is attached.");
            _logger.LogInformation($"");
            _logger.LogInformation($"Best regards,");
            _logger.LogInformation($"{interview.Application?.Job?.Company?.Name} Recruitment Team");
            _logger.LogInformation("=====================================================");

            await Task.CompletedTask;
        }

        private async Task LoadInterviewDataAsync(Interview interview)
        {
            if (interview.Application == null)
            {
                var loadedInterview = await _context.Interviews
                    .Include(i => i.Application)
                        .ThenInclude(a => a!.Applicant)
                    .Include(i => i.Application)
                        .ThenInclude(a => a!.Job)
                            .ThenInclude(j => j!.Company)
                    .Include(i => i.Recruiter)
                    .FirstOrDefaultAsync(i => i.Id == interview.Id);

                if (loadedInterview != null)
                {
                    interview.Application = loadedInterview.Application;
                    interview.Recruiter = loadedInterview.Recruiter;
                }
            }
        }

        private async Task LoadApplicationDataAsync(Application application)
        {
            if (application.Applicant == null || application.Job == null)
            {
                var loadedApplication = await _context.Applications
                    .Include(a => a.Applicant)
                    .Include(a => a.Job)
                        .ThenInclude(j => j!.Company)
                    .FirstOrDefaultAsync(a => a.Id == application.Id);

                if (loadedApplication != null)
                {
                    application.Applicant = loadedApplication.Applicant;
                    application.Job = loadedApplication.Job;
                }
            }
        }
    }
}
