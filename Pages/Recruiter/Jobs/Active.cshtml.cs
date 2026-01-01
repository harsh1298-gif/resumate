using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter.Jobs
{
    [Authorize(Roles = "Recruiter")]
    public class ActiveModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ActiveModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Models.Recruiter? CurrentRecruiter { get; set; }
        public List<JobListItemViewModel> Jobs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; }

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

            // Build query for jobs
            var query = _context.Jobs
                .Where(j => j.CompanyId == CurrentRecruiter.CompanyId)
                .Include(j => j.Applications)
                    .ThenInclude(a => a.Applicant)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                if (StatusFilter == "Active")
                    query = query.Where(j => j.IsActive);
                else if (StatusFilter == "Inactive")
                    query = query.Where(j => !j.IsActive);
            }

            // Apply sorting
            query = SortBy switch
            {
                "Title" => query.OrderBy(j => j.Title),
                "Date" => query.OrderByDescending(j => j.PostedDate),
                "Applicants" => query.OrderByDescending(j => j.Applications.Count),
                _ => query.OrderByDescending(j => j.CreatedAt)
            };

            var jobs = await query.ToListAsync();

            // Map to view model
            Jobs = jobs.Select(j => new JobListItemViewModel
            {
                Id = j.Id,
                Title = j.Title,
                Location = j.Location,
                Type = j.Type,
                ExperienceLevel = j.ExperienceLevel,
                PostedDate = j.PostedDate,
                ApplicationDeadline = j.ApplicationDeadline,
                IsActive = j.IsActive,
                TotalApplicants = j.Applications.Count,
                PendingApplicants = j.Applications.Count(a => a.Status == "Pending" || a.Status == "Under Review"),
                ShortlistedApplicants = j.Applications.Count(a => a.Status == "Shortlisted"),
                HiredApplicants = j.Applications.Count(a => a.Status == "Hired")
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (recruiter == null)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToPage();
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == recruiter.CompanyId);

            if (job == null)
            {
                TempData["Error"] = "Job not found or you don't have permission to modify it.";
                return RedirectToPage();
            }

            job.IsActive = !job.IsActive;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = job.IsActive ? "Job posting activated successfully!" : "Job posting deactivated successfully!";
            return RedirectToPage();
        }

        public class JobListItemViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public JobType Type { get; set; }
            public ExperienceLevel ExperienceLevel { get; set; }
            public DateTime PostedDate { get; set; }
            public DateTime? ApplicationDeadline { get; set; }
            public bool IsActive { get; set; }
            public int TotalApplicants { get; set; }
            public int PendingApplicants { get; set; }
            public int ShortlistedApplicants { get; set; }
            public int HiredApplicants { get; set; }
        }
    }
}
