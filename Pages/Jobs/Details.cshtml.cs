using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Jobs
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Job Job { get; set; } = null!;
        public bool IsAuthenticated { get; set; }
        public bool IsApplicant { get; set; }
        public bool HasAlreadyApplied { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Job = await _context.Jobs
                .Include(j => j.Company)
                .Include(j => j.Recruiter)
                .Include(j => j.RequiredSkills)
                    .ThenInclude(rs => rs.Skill)
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == id.Value);

            if (Job == null)
            {
                return NotFound();
            }

            // Check user authentication and role
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    IsApplicant = await _userManager.IsInRoleAsync(user, "Applicant");

                    if (IsApplicant)
                    {
                        // Check if user has already applied
                        var applicant = await _context.Applicants
                            .FirstOrDefaultAsync(a => a.Email == user.Email);

                        if (applicant != null)
                        {
                            HasAlreadyApplied = await _context.Applications
                                .AnyAsync(a => a.ApplicantId == applicant.Id && a.JobId == Job.Id);
                        }
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostApplyAsync(int jobId)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToPage("/Login", new { returnUrl = $"/Jobs/Details?id={jobId}" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            // Get applicant
            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.Email == user.Email);

            if (applicant == null)
            {
                TempData["Error"] = "Applicant profile not found. Please complete your profile first.";
                return RedirectToPage("/Applicant/Dashboard");
            }

            // Check if already applied
            var existingApplication = await _context.Applications
                .FirstOrDefaultAsync(a => a.ApplicantId == applicant.Id && a.JobId == jobId);

            if (existingApplication != null)
            {
                TempData["Error"] = "You have already applied to this job.";
                return RedirectToPage("/Jobs/Details", new { id = jobId });
            }

            // Create new application
            var application = new Application
            {
                ApplicantId = applicant.Id,
                JobId = jobId,
                ApplicationDate = DateTime.UtcNow,
                Status = ApplicationStatus.Pending
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application submitted successfully!";
            return RedirectToPage("/Jobs/Details", new { id = jobId });
        }
    }
}
