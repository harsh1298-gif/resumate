using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter
{
    [Authorize(Roles = "Recruiter")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string RecruiterName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int ActiveJobsCount { get; set; }
        public int TotalApplicationsCount { get; set; }
        public int PendingApplicationsCount { get; set; }
        public int ApplicationsThisMonth { get; set; }

        public List<Job> RecentJobs { get; set; } = new();
        public List<Application> RecentApplications { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            // Get recruiter info
            var recruiter = await _context.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.Email == user.Email);

            if (recruiter == null)
            {
                return RedirectToPage("/Error");
            }

            RecruiterName = recruiter.Name;
            CompanyName = recruiter.Company?.Name ?? "Your Company";

            // Get jobs posted by this recruiter
            var recruiterJobs = await _context.Jobs
                .Include(j => j.Applications)
                    .ThenInclude(a => a.Applicant)
                .Where(j => j.RecruiterId == recruiter.Id)
                .ToListAsync();

            // Calculate stats
            ActiveJobsCount = recruiterJobs.Count(j => j.IsActive);
            TotalApplicationsCount = recruiterJobs.Sum(j => j.Applications.Count);
            PendingApplicationsCount = recruiterJobs
                .SelectMany(j => j.Applications)
                .Count(a => a.Status == ApplicationStatus.Pending);

            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            ApplicationsThisMonth = recruiterJobs
                .SelectMany(j => j.Applications)
                .Count(a => a.ApplicationDate >= firstDayOfMonth);

            // Get recent jobs (top 5)
            RecentJobs = recruiterJobs
                .OrderByDescending(j => j.PostedDate)
                .Take(5)
                .ToList();

            // Get recent applications (top 10)
            RecentApplications = recruiterJobs
                .SelectMany(j => j.Applications)
                .OrderByDescending(a => a.ApplicationDate)
                .Take(10)
                .ToList();

            return Page();
        }
    }
}
