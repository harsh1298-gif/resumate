using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using RESUMATE_FINAL_WORKING_MODEL.Services;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter.Interviews
{
    [Authorize(Roles = "Recruiter")]
    public class ScheduleModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IIcsCalendarService _icsService;

        public ScheduleModel(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            IIcsCalendarService icsService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _icsService = icsService;
        }

        public Models.Recruiter? CurrentRecruiter { get; set; }
        public List<SelectListItem> CandidateOptions { get; set; } = new();

        [BindProperty]
        public InterviewInputModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? applicationId)
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

            // Load shortlisted candidates
            var candidates = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Job)
                .Where(a => a.Job!.CompanyId == CurrentRecruiter.CompanyId &&
                           (a.Status == "Shortlisted" || a.Status == "Under Review" || a.Status == "Pending"))
                .ToListAsync();

            CandidateOptions = candidates.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Applicant?.FullName} - {a.Job?.Title}",
                Selected = applicationId.HasValue && a.Id == applicationId.Value
            }).ToList();

            // Pre-select if applicationId provided
            if (applicationId.HasValue)
            {
                Input.ApplicationId = applicationId.Value;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            CurrentRecruiter = await _context.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (CurrentRecruiter == null)
            {
                TempData["Error"] = "Recruiter profile not found.";
                return RedirectToPage("/RecruiterDashboard");
            }

            if (!ModelState.IsValid)
            {
                // Reload candidates
                await LoadCandidatesAsync();
                return Page();
            }

            // Validate interview time is in future
            if (Input.ScheduledDateTime <= DateTime.Now.AddHours(1))
            {
                ModelState.AddModelError("Input.ScheduledDateTime", "Interview must be scheduled at least 1 hour in the future.");
                await LoadCandidatesAsync();
                return Page();
            }

            // Get application
            var application = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Job)
                    .ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(a => a.Id == Input.ApplicationId);

            if (application == null || application.Job?.CompanyId != CurrentRecruiter.CompanyId)
            {
                TempData["Error"] = "Application not found or unauthorized access.";
                return RedirectToPage();
            }

            // Check if applicant is rejected or hired
            if (application.Status == "Rejected" || application.Status == "Hired")
            {
                TempData["Error"] = $"Cannot schedule interview for {application.Status.ToLower()} applicants.";
                return RedirectToPage();
            }

            // Create interview
            var interview = new Interview
            {
                ApplicationId = Input.ApplicationId,
                RecruiterId = CurrentRecruiter.Id,
                ScheduledDateTime = Input.ScheduledDateTime,
                DurationMinutes = Input.DurationMinutes,
                Type = Input.Type,
                Round = Input.Round,
                Status = InterviewStatus.Scheduled,
                MeetingLink = Input.MeetingLink,
                Location = Input.Location,
                SpecialInstructions = Input.SpecialInstructions,
                CreatedAt = DateTime.UtcNow
            };

            _context.Interviews.Add(interview);

            // Update application status
            application.Status = "Interview Scheduled";
            application.StatusChangedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Load full interview data for email
            interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a!.Applicant)
                .Include(i => i.Application)
                    .ThenInclude(a => a!.Job)
                        .ThenInclude(j => j!.Company)
                .Include(i => i.Recruiter)
                .FirstOrDefaultAsync(i => i.Id == interview.Id);

            // Send email notification with calendar file
            try
            {
                await _emailService.SendInterviewInvitationAsync(interview!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }

            TempData["Success"] = $"Interview scheduled successfully with {application.Applicant?.FullName}!";
            return RedirectToPage("/Recruiter/Interviews/List");
        }

        private async Task LoadCandidatesAsync()
        {
            var candidates = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Job)
                .Where(a => a.Job!.CompanyId == CurrentRecruiter!.CompanyId &&
                           (a.Status == "Shortlisted" || a.Status == "Under Review" || a.Status == "Pending"))
                .ToListAsync();

            CandidateOptions = candidates.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Applicant?.FullName} - {a.Job?.Title}"
            }).ToList();
        }

        public class InterviewInputModel
        {
            [Required(ErrorMessage = "Please select a candidate")]
            public int ApplicationId { get; set; }

            [Required(ErrorMessage = "Interview type is required")]
            public InterviewType Type { get; set; }

            [Required(ErrorMessage = "Interview round is required")]
            public InterviewRound Round { get; set; }

            [Required(ErrorMessage = "Interview date and time is required")]
            public DateTime ScheduledDateTime { get; set; } = DateTime.Now.AddDays(1);

            [Required(ErrorMessage = "Duration is required")]
            [Range(15, 480, ErrorMessage = "Duration must be between 15 minutes and 8 hours")]
            public int DurationMinutes { get; set; } = 60;

            [StringLength(500)]
            public string? MeetingLink { get; set; }

            [StringLength(500)]
            public string? Location { get; set; }

            [StringLength(2000)]
            public string? SpecialInstructions { get; set; }
        }
    }
}
