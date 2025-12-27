using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    [Authorize(Roles = "Applicant")]
    public class ApplyJobModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApplyJobModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ApplicationInput Input { get; set; } = new ApplicationInput();

        public JobSummaryViewModel Job { get; set; } = new JobSummaryViewModel();
        public ApplicantSummaryViewModel Applicant { get; set; } = new ApplicantSummaryViewModel();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int jobId)
        {
            // Get job details
            var job = await _context.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return NotFound();
            }

            // Check if job is still active
            if (!job.IsActive || (job.ClosingDate.HasValue && job.ClosingDate < DateTime.Now))
            {
                TempData["Error"] = "This job posting has been closed.";
                return RedirectToPage("/JobDetails", new { id = jobId });
            }

            // Get current user's applicant profile
            var user = await _userManager.GetUserAsync(User);
            var applicant = await _context.Applicants
                .Include(a => a.ApplicantSkills)
                    .ThenInclude(ask => ask.Skill)
                .Include(a => a.Experiences)
                .Include(a => a.Educations)
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (applicant == null)
            {
                TempData["Error"] = "Please complete your profile before applying for jobs.";
                return RedirectToPage("/EditProfile");
            }

            // Check if already applied
            var existingApplication = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.ApplicantId == applicant.Id);

            if (existingApplication != null)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToPage("/JobDetails", new { id = jobId });
            }

            // Map job to view model
            Job = new JobSummaryViewModel
            {
                Id = job.Id,
                Title = job.Title,
                CompanyName = job.Company?.Name ?? "Unknown Company",
                CompanyLogo = job.Company?.LogoPath ?? "/images/default-company.png",
                Location = job.Location,
                Type = job.Type.ToString(),
                Salary = job.Salary
            };

            // Map applicant to view model
            Applicant = new ApplicantSummaryViewModel
            {
                FullName = applicant.FullName,
                Email = applicant.Email,
                PhoneNumber = applicant.PhoneNumber,
                ProfilePhotoPath = applicant.ProfilePhotoPath ?? "/images/default-avatar.png",
                ResumeFileName = applicant.ResumeFileName,
                ResumeFilePath = applicant.ResumeFilePath,
                HasResume = applicant.HasResume,
                TotalExperience = applicant.TotalExperienceYears,
                SkillsCount = applicant.ApplicantSkills?.Count ?? 0,
                IsProfileComplete = applicant.IsProfileComplete
            };

            // Pre-fill input with applicant data
            Input.JobId = jobId;
            Input.CoverLetter = string.Empty;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync(Input.JobId);
                return Page();
            }

            // Get current user's applicant profile
            var user = await _userManager.GetUserAsync(User);
            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (applicant == null)
            {
                TempData["Error"] = "Applicant profile not found.";
                return RedirectToPage("/EditProfile");
            }

            // Verify job is still active
            var job = await _context.Jobs.FindAsync(Input.JobId);
            if (job == null || !job.IsActive || (job.ClosingDate.HasValue && job.ClosingDate < DateTime.Now))
            {
                TempData["Error"] = "This job posting is no longer available.";
                return RedirectToPage("/BrowseJobs");
            }

            // Check for duplicate application
            var existingApplication = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobId == Input.JobId && a.ApplicantId == applicant.Id);

            if (existingApplication != null)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToPage("/JobDetails", new { id = Input.JobId });
            }

            // Create application
            var application = new Application
            {
                JobId = Input.JobId,
                ApplicantId = applicant.Id,
                ApplicationDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your application has been submitted successfully!";
            return RedirectToPage("/MyApplications");
        }

        private async Task LoadPageDataAsync(int jobId)
        {
            var job = await _context.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job != null)
            {
                Job = new JobSummaryViewModel
                {
                    Id = job.Id,
                    Title = job.Title,
                    CompanyName = job.Company?.Name ?? "Unknown Company",
                    CompanyLogo = job.Company?.LogoPath ?? "/images/default-company.png",
                    Location = job.Location,
                    Type = job.Type.ToString(),
                    Salary = job.Salary
                };
            }

            var user = await _userManager.GetUserAsync(User);
            var applicant = await _context.Applicants
                .Include(a => a.ApplicantSkills)
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (applicant != null)
            {
                Applicant = new ApplicantSummaryViewModel
                {
                    FullName = applicant.FullName,
                    Email = applicant.Email,
                    PhoneNumber = applicant.PhoneNumber,
                    ProfilePhotoPath = applicant.ProfilePhotoPath ?? "/images/default-avatar.png",
                    ResumeFileName = applicant.ResumeFileName,
                    ResumeFilePath = applicant.ResumeFilePath,
                    HasResume = applicant.HasResume,
                    TotalExperience = applicant.TotalExperienceYears,
                    SkillsCount = applicant.ApplicantSkills?.Count ?? 0,
                    IsProfileComplete = applicant.IsProfileComplete
                };
            }
        }

        public string GetFormattedSalary(decimal? salary)
        {
            if (!salary.HasValue)
                return "Salary not disclosed";

            if (salary >= 100000)
                return $"?{salary / 100000:F1}L per year";
            else if (salary >= 1000)
                return $"?{salary / 1000:F0}K per year";
            else
                return $"?{salary:N0} per year";
        }
    }

    public class ApplicationInput
    {
        [Required]
        public int JobId { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Cover Letter (Optional)")]
        public string? CoverLetter { get; set; }

        [Required]
        [Display(Name = "I confirm that all information provided is accurate")]
        public bool ConfirmAccuracy { get; set; }
    }

    public class JobSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
    }

    public class ApplicantSummaryViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ProfilePhotoPath { get; set; } = string.Empty;
        public string? ResumeFileName { get; set; }
        public string? ResumeFilePath { get; set; }
        public bool HasResume { get; set; }
        public int TotalExperience { get; set; }
        public int SkillsCount { get; set; }
        public bool IsProfileComplete { get; set; }
    }
}