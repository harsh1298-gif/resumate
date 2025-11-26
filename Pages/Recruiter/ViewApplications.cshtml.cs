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
    public class ViewApplicationsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ViewApplicationsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Job Job { get; set; } = null!;
        public List<Application> Applications { get; set; } = new();
        public List<Application> FilteredApplications { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task<IActionResult> OnGetAsync(int? jobId, string? status)
        {
            if (jobId == null)
            {
                return NotFound();
            }

            StatusFilter = status;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.Email == user.Email);

            if (recruiter == null)
            {
                return RedirectToPage("/Index");
            }

            // Get job with applications
            Job = await _context.Jobs
                .Include(j => j.Company)
                .Include(j => j.Applications)
                    .ThenInclude(a => a.Applicant)
                        .ThenInclude(ap => ap.Skills)
                            .ThenInclude(s => s.Skill)
                .FirstOrDefaultAsync(j => j.Id == jobId.Value);

            if (Job == null)
            {
                return NotFound();
            }

            // Verify ownership
            if (Job.RecruiterId != recruiter.Id)
            {
                TempData["Error"] = "You don't have permission to view these applications.";
                return RedirectToPage("/Recruiter/Dashboard");
            }

            Applications = Job.Applications.OrderByDescending(a => a.ApplicationDate).ToList();

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<ApplicationStatus>(StatusFilter, out var statusEnum))
            {
                FilteredApplications = Applications.Where(a => a.Status == statusEnum).ToList();
            }
            else
            {
                FilteredApplications = Applications;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int applicationId, string newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.Email == user.Email);

            if (recruiter == null)
            {
                return RedirectToPage("/Index");
            }

            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            // Verify ownership
            if (application.Job.RecruiterId != recruiter.Id)
            {
                TempData["Error"] = "You don't have permission to update this application.";
                return RedirectToPage("/Recruiter/Dashboard");
            }

            // Update status
            if (Enum.TryParse<ApplicationStatus>(newStatus, out var statusEnum))
            {
                application.Status = statusEnum;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Application status updated to {statusEnum}";
            }
            else
            {
                TempData["Error"] = "Invalid status value.";
            }

            return RedirectToPage("/Recruiter/ViewApplications", new { jobId = application.JobId, status = StatusFilter });
        }
    }
}
