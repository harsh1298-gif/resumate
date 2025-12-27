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
    public class BrowseJobsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BrowseJobsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Filter Properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Location { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? JobType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ExperienceLevel { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinSalary { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 12;

        // Data Properties
        public List<JobViewModel> Jobs { get; set; } = new List<JobViewModel>();
        public int TotalJobs { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // Filter Options
        public List<string> Locations { get; set; } = new List<string>();
        public List<string> JobTypes { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> ExperienceLevels { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get all active jobs
            var jobsQuery = _context.Jobs
                .Include(j => j.Company)
                .Include(j => j.Applications)
                .Where(j => j.IsActive && (j.ClosingDate == null || j.ClosingDate > DateTime.Now))
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                jobsQuery = jobsQuery.Where(j =>
                    j.Title.Contains(SearchTerm) ||
                    j.Description.Contains(SearchTerm) ||
                    j.Company.Name.Contains(SearchTerm));
            }

            if (!string.IsNullOrEmpty(Location))
            {
                jobsQuery = jobsQuery.Where(j => j.Location.Contains(Location));
            }

            if (!string.IsNullOrEmpty(JobType) && Enum.TryParse<JobType>(JobType, out var jobTypeEnum))
            {
                jobsQuery = jobsQuery.Where(j => j.Type == jobTypeEnum);
            }

            if (!string.IsNullOrEmpty(Category) && Enum.TryParse<JobCategory>(Category, out var categoryEnum))
            {
                jobsQuery = jobsQuery.Where(j => j.Category == categoryEnum);
            }

            if (!string.IsNullOrEmpty(ExperienceLevel) && Enum.TryParse<ExperienceLevel>(ExperienceLevel, out var expLevelEnum))
            {
                jobsQuery = jobsQuery.Where(j => j.ExperienceLevel == expLevelEnum);
            }

            if (MinSalary.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.Salary >= MinSalary);
            }

            // Get total count for pagination
            TotalJobs = await jobsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalJobs / (double)PageSize);

            // Get paginated results
            var jobs = await jobsQuery
                .OrderByDescending(j => j.PostedDate)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Get current user's applicant ID to check if already applied
            var user = await _userManager.GetUserAsync(User);
            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            // Map to view model
            Jobs = jobs.Select(j => new JobViewModel
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description.Length > 200 ? j.Description.Substring(0, 200) + "..." : j.Description,
                Location = j.Location,
                Salary = j.Salary,
                Type = j.Type.ToString(),
                Category = j.Category.ToString(),
                ExperienceLevel = j.ExperienceLevel.ToString(),
                CompanyName = j.Company?.Name ?? "Unknown Company",
                CompanyLogo = j.Company?.LogoPath ?? "/images/default-company.png",
                PostedDate = j.PostedDate,
                ClosingDate = j.ClosingDate,
                ApplicationCount = j.Applications.Count,
                HasApplied = applicant != null && j.Applications.Any(a => a.ApplicantId == applicant.Id)
            }).ToList();

            // Load filter options
            await LoadFilterOptionsAsync();

            return Page();
        }

        private async Task LoadFilterOptionsAsync()
        {
            var allJobs = await _context.Jobs
                .Where(j => j.IsActive)
                .Include(j => j.Company)
                .ToListAsync();

            Locations = allJobs
                .Select(j => j.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            JobTypes = Enum.GetNames(typeof(JobType)).ToList();
            Categories = Enum.GetNames(typeof(JobCategory)).ToList();
            ExperienceLevels = Enum.GetNames(typeof(ExperienceLevel)).ToList();
        }

        public string GetRelativeTime(DateTime date)
        {
            var timeSpan = DateTime.Now - date;

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

        public string GetFormattedSalary(decimal? salary)
        {
            if (!salary.HasValue)
                return "Salary not disclosed";

            if (salary >= 100000)
                return $"₹{salary / 100000:F1}L per year";
            else if (salary >= 1000)
                return $"₹{salary / 1000:F0}K per year";
            else
                return $"₹{salary:N0} per year";
        }
    }

    public class JobViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ExperienceLevel { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public DateTime PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int ApplicationCount { get; set; }
        public bool HasApplied { get; set; }
    }
}
