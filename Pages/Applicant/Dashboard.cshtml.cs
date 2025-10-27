using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Applicant
{
    [Authorize(Roles = "Applicant")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            RecentApplications = new List<ApplicationViewModel>();
        }

        public string ApplicantName { get; set; } = string.Empty;
        public int ProfileCompletionPercentage { get; set; }
        public List<ApplicationViewModel> RecentApplications { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (applicant == null)
            {
                return NotFound("Applicant profile not found.");
            }

            // Set basic info
            ApplicantName = string.IsNullOrEmpty(applicant.FullName) ? "Applicant" : applicant.FullName;
            
            // Calculate profile completion percentage
            ProfileCompletionPercentage = CalculateProfileCompletion(applicant);

            // Get recent applications
            RecentApplications = await _context.Applications
                .Where(a => a.ApplicantId == applicant.Id)
                .OrderByDescending(a => a.ApplicationDate)
                .Take(5)
                .Select(a => new ApplicationViewModel
                {
                    JobTitle = a.Job.Title,
                    CompanyName = a.Job.Company.Name,
                    AppliedDate = a.ApplicationDate,
                    Status = a.Status
                })
                .ToListAsync();

            return Page();
        }

        private int CalculateProfileCompletion(Models.Applicant applicant)
        {
            int completedFields = 0;
            int totalFields = 8; // Total number of fields we're checking

            if (!string.IsNullOrEmpty(applicant.FullName)) completedFields++;
            if (!string.IsNullOrEmpty(applicant.Email)) completedFields++;
            if (!string.IsNullOrEmpty(applicant.PhoneNumber)) completedFields++;
            if (applicant.DateOfBirth != default) completedFields++;
            if (!string.IsNullOrEmpty(applicant.Address)) completedFields++;
            if (!string.IsNullOrEmpty(applicant.ProfessionalSummary)) completedFields++;
            if (!string.IsNullOrEmpty(applicant.ResumeFileName)) completedFields++;
            if (applicant.Skills?.Any() == true) completedFields++;

            return (int)((double)completedFields / totalFields * 100);
        }
    }

    public class ApplicationViewModel
    {
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; } = "Submitted";
    }
}
