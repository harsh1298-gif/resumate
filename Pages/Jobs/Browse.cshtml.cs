using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Jobs
{
    public class BrowseModel : PageModel
    {
        private readonly AppDbContext _context;
        private const int PageSize = 10;

        public BrowseModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Job> Jobs { get; set; } = new();
        public int TotalJobs { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;

        // Filter parameters
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
        public decimal? MaxSalary { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "newest";

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentPage = Page;

            // Start with all active jobs
            var query = _context.Jobs
                .Include(j => j.Company)
                .Include(j => j.RequiredSkills)
                    .ThenInclude(rs => rs.Skill)
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive);

            // Apply search term filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(j =>
                    j.Title.ToLower().Contains(searchLower) ||
                    j.Description.ToLower().Contains(searchLower) ||
                    j.Company.Name.ToLower().Contains(searchLower) ||
                    j.RequiredSkills.Any(rs => rs.Skill.Name.ToLower().Contains(searchLower))
                );
            }

            // Apply location filter
            if (!string.IsNullOrWhiteSpace(Location))
            {
                var locationLower = Location.ToLower();
                query = query.Where(j => j.Location.ToLower().Contains(locationLower));
            }

            // Apply job type filter
            if (!string.IsNullOrWhiteSpace(JobType) && Enum.TryParse<Models.JobType>(JobType, out var jobTypeEnum))
            {
                query = query.Where(j => j.Type == jobTypeEnum);
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(Category) && Enum.TryParse<JobCategory>(Category, out var categoryEnum))
            {
                query = query.Where(j => j.Category == categoryEnum);
            }

            // Apply experience level filter
            if (!string.IsNullOrWhiteSpace(ExperienceLevel) && Enum.TryParse<Models.ExperienceLevel>(ExperienceLevel, out var experienceEnum))
            {
                query = query.Where(j => j.ExperienceLevel == experienceEnum);
            }

            // Apply salary range filters
            if (MinSalary.HasValue)
            {
                query = query.Where(j => j.Salary.HasValue && j.Salary >= MinSalary.Value);
            }

            if (MaxSalary.HasValue)
            {
                query = query.Where(j => j.Salary.HasValue && j.Salary <= MaxSalary.Value);
            }

            // Get total count before pagination
            TotalJobs = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalJobs / (double)PageSize);

            // Apply sorting
            query = SortBy switch
            {
                "salary-high" => query.OrderByDescending(j => j.Salary),
                "salary-low" => query.OrderBy(j => j.Salary),
                "relevance" => query.OrderByDescending(j => j.PostedDate), // TODO: Implement proper relevance scoring
                _ => query.OrderByDescending(j => j.PostedDate) // newest (default)
            };

            // Apply pagination
            Jobs = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }
    }
}
