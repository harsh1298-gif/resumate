using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    [Authorize(Roles = "Applicant")]
    public class JobDetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public JobDetailsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public JobDetailViewModel Job { get; set; } = new JobDetailViewModel();
        public bool HasApplied { get; set; }
        public bool CanApply { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Company)
                .Include(j => j.Recruiter)
                .Include(j => j.RequiredSkills!)
                    .ThenInclude(js => js.Skill)
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Check if job is still active
            if (!job.IsActive || (job.ClosingDate.HasValue && job.ClosingDate < DateTime.Now))
            {
                ErrorMessage = "This job posting has been closed.";
                CanApply = false;
            }

            // Get current user's applicant profile
            var user = await _userManager.GetUserAsync(User);
            var applicant = await _context.Applicants
                .Include(a => a.ApplicantSkills)
                    .ThenInclude(ask => ask.Skill)
                .FirstOrDefaultAsync(a => a.UserId == user!.Id);

            int matchPercentage = 0;

            if (applicant == null)
            {
                ErrorMessage = "Please complete your profile before applying for jobs.";
                CanApply = false;
            }
            else
            {
                // Check if already applied
                HasApplied = job.Applications.Any(a => a.ApplicantId == applicant.Id);
                CanApply = !HasApplied && job.IsActive && (!job.ClosingDate.HasValue || job.ClosingDate > DateTime.Now);

                // Calculate match score
                if (job.RequiredSkills?.Any() == true && applicant.ApplicantSkills?.Any() == true)
                {
                    var requiredSkillIds = job.RequiredSkills.Select(js => js.SkillId).ToList();
                    var applicantSkillIds = applicant.ApplicantSkills.Select(ask => ask.SkillId).ToList();
                    var matchingSkills = requiredSkillIds.Intersect(applicantSkillIds).Count();

                    if (requiredSkillIds.Count > 0)
                    {
                        matchPercentage = (int)((matchingSkills / (double)requiredSkillIds.Count) * 100);
                    }
                }
            }

            // Map to view model
            Job = new JobDetailViewModel
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Location = job.Location,
                Salary = job.Salary,
                Type = job.Type.ToString(),
                Category = job.Category.ToString(),
                ExperienceLevel = job.ExperienceLevel.ToString(),
                CompanyId = job.CompanyId,
                CompanyName = job.Company?.Name ?? "Unknown Company",
                CompanyLogo = job.Company?.LogoPath ?? "/images/default-company.png",
                CompanyDescription = job.Company?.Description ?? string.Empty,
                CompanyWebsite = job.Company?.Website,
                RecruiterName = job.Recruiter?.Name ?? "Recruiter",
                PostedDate = job.PostedDate,
                ClosingDate = job.ClosingDate,
                IsActive = job.IsActive,
                ApplicationCount = job.Applications?.Count ?? 0,
                RequiredSkills = job.RequiredSkills?
                    .Where(rs => rs.Skill != null)
                    .Select(rs => rs.Skill!.Name ?? "Unknown")
                    .ToList() ?? new List<string>(),
                MatchPercentage = matchPercentage
            };

            return Page();
        }

        // Helper method to format salary
        public string GetFormattedSalary(decimal? salary)
        {
            if (!salary.HasValue)
                return "Not disclosed";

            if (salary.Value >= 100000)
                return $"₹{salary.Value / 100000:F1}L per year";
            else if (salary.Value >= 1000)
                return $"₹{salary.Value / 1000:F0}K per year";
            else
                return $"₹{salary.Value:N0} per year";
        }

        // Helper method to format relative time
        public string GetRelativeTime(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;

            if (timeSpan.TotalDays < 1)
                return "Today";
            else if (timeSpan.TotalDays < 2)
                return "Yesterday";
            else if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            else if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            else if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            else
                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }

        // Helper method to get days remaining
        public int GetDaysRemaining(DateTime? closingDate)
        {
            if (!closingDate.HasValue)
                return 30; // Default if no closing date

            var timeSpan = closingDate.Value - DateTime.UtcNow;
            return Math.Max(0, (int)timeSpan.TotalDays);
        }
    }

    // View Model
    public class JobDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ExperienceLevel { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string CompanyDescription { get; set; } = string.Empty;
        public string? CompanyWebsite { get; set; }
        public string RecruiterName { get; set; } = string.Empty;
        public DateTime PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public bool IsActive { get; set; }
        public int ApplicationCount { get; set; }
        public List<string> RequiredSkills { get; set; } = new List<string>();
        public int MatchPercentage { get; set; }
    }
}