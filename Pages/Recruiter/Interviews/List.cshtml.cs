using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using RESUMATE_FINAL_WORKING_MODEL.Services;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter.Interviews
{
    [Authorize(Roles = "Recruiter")]
    public class ListModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public ListModel(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public Models.Recruiter? CurrentRecruiter { get; set; }
        public List<InterviewViewModel> Interviews { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? View { get; set; } = "list"; // "list" or "calendar"

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Month { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get current recruiter
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Login");

            CurrentRecruiter = await _context.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (CurrentRecruiter == null)
            {
                TempData["Error"] = "Recruiter profile not found.";
                return RedirectToPage("/RecruiterDashboard");
            }

            // Set default month if not provided
            if (!Month.HasValue)
                Month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Build query
            var query = _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a!.Applicant)
                .Include(i => i.Application)
                    .ThenInclude(a => a!.Job)
                .Where(i => i.RecruiterId == CurrentRecruiter.Id);

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                if (Enum.TryParse<InterviewStatus>(StatusFilter, out var status))
                {
                    query = query.Where(i => i.Status == status);
                }
            }

            // For calendar view, filter by month
            if (View == "calendar" && Month.HasValue)
            {
                var monthStart = Month.Value;
                var monthEnd = monthStart.AddMonths(1);
                query = query.Where(i => i.ScheduledDateTime >= monthStart && i.ScheduledDateTime < monthEnd);
            }

            // Get interviews
            var interviews = await query
                .OrderByDescending(i => i.ScheduledDateTime)
                .ToListAsync();

            // Map to view model
            Interviews = interviews.Select(i => new InterviewViewModel
            {
                Id = i.Id,
                CandidateName = i.Application?.Applicant?.FullName ?? "Unknown",
                JobTitle = i.Application?.Job?.Title ?? "",
                ScheduledDateTime = i.ScheduledDateTime,
                DurationMinutes = i.DurationMinutes,
                Type = i.Type,
                Round = i.Round,
                Status = i.Status,
                MeetingLink = i.MeetingLink,
                Location = i.Location
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int interviewId, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (recruiter == null)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToPage();
            }

            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a!.Applicant)
                .Include(i => i.Application)
                    .ThenInclude(a => a!.Job)
                .FirstOrDefaultAsync(i => i.Id == interviewId && i.RecruiterId == recruiter.Id);

            if (interview == null)
            {
                TempData["Error"] = "Interview not found.";
                return RedirectToPage();
            }

            interview.Status = InterviewStatus.Cancelled;
            interview.CancelledAt = DateTime.UtcNow;
            interview.CancellationReason = reason;

            await _context.SaveChangesAsync();

            // Send cancellation email
            try
            {
                await _emailService.SendInterviewCancellationAsync(interview);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }

            TempData["Success"] = "Interview cancelled successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteAsync(int interviewId, string? feedback, int? rating)
        {
            var user = await _userManager.GetUserAsync(User);
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (recruiter == null)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToPage();
            }

            var interview = await _context.Interviews
                .FirstOrDefaultAsync(i => i.Id == interviewId && i.RecruiterId == recruiter.Id);

            if (interview == null)
            {
                TempData["Error"] = "Interview not found.";
                return RedirectToPage();
            }

            interview.Status = InterviewStatus.Completed;
            interview.CompletedAt = DateTime.UtcNow;
            interview.Feedback = feedback;
            interview.Rating = rating;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Interview marked as completed.";
            return RedirectToPage();
        }

        public class InterviewViewModel
        {
            public int Id { get; set; }
            public string CandidateName { get; set; } = string.Empty;
            public string JobTitle { get; set; } = string.Empty;
            public DateTime ScheduledDateTime { get; set; }
            public int DurationMinutes { get; set; }
            public InterviewType Type { get; set; }
            public InterviewRound Round { get; set; }
            public InterviewStatus Status { get; set; }
            public string? MeetingLink { get; set; }
            public string? Location { get; set; }
        }
    }
}
