using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter
{
    [Authorize(Roles = "Recruiter")]
    public class SearchApplicantsModel : PageModel
    {
        private readonly AppDbContext _context;
        private const int PageSize = 12;

        public SearchApplicantsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Applicant> Applicants { get; set; } = new();
        public int TotalApplicants { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;

        // Filter parameters
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Location { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Skills { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MinExperience { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EducationLevel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "relevance";

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentPage = Page;

            // Start with all active applicants with complete profiles
            var query = _context.Applicants
                .Include(a => a.Skills)
                    .ThenInclude(s => s.Skill)
                .Include(a => a.Experiences)
                .Include(a => a.Education)
                .Where(a => a.IsActive && a.IsProfileComplete);

            // Apply search term filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(a =>
                    a.FullName.ToLower().Contains(searchLower) ||
                    (a.Objective != null && a.Objective.ToLower().Contains(searchLower)) ||
                    (a.ProfessionalSummary != null && a.ProfessionalSummary.ToLower().Contains(searchLower)) ||
                    a.Skills.Any(s => s.Skill.Name.ToLower().Contains(searchLower)) ||
                    a.Experiences.Any(e =>
                        e.JobTitle.ToLower().Contains(searchLower) ||
                        e.CompanyName.ToLower().Contains(searchLower)) ||
                    a.Education.Any(e =>
                        e.Degree.ToLower().Contains(searchLower) ||
                        (e.FieldOfStudy != null && e.FieldOfStudy.ToLower().Contains(searchLower)))
                );
            }

            // Apply location filter
            if (!string.IsNullOrWhiteSpace(Location))
            {
                var locationLower = Location.ToLower();
                query = query.Where(a =>
                    (a.City != null && a.City.ToLower().Contains(locationLower)) ||
                    (a.Address != null && a.Address.ToLower().Contains(locationLower))
                );
            }

            // Apply skills filter
            if (!string.IsNullOrWhiteSpace(Skills))
            {
                var skillsList = Skills.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLower())
                    .ToList();

                if (skillsList.Any())
                {
                    query = query.Where(a => a.Skills.Any(s =>
                        skillsList.Contains(s.Skill.Name.ToLower())
                    ));
                }
            }

            // Apply minimum experience filter
            if (MinExperience.HasValue && MinExperience.Value > 0)
            {
                query = query.Where(a => a.TotalExperienceYears >= MinExperience.Value);
            }

            // Apply education level filter
            if (!string.IsNullOrWhiteSpace(EducationLevel))
            {
                query = query.Where(a => a.Education.Any(e =>
                    e.Degree.Contains(EducationLevel)
                ));
            }

            // Get total count before pagination
            TotalApplicants = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalApplicants / (double)PageSize);

            // Apply sorting
            query = SortBy switch
            {
                "experience" => query.OrderByDescending(a => a.TotalExperienceYears),
                "recent" => query.OrderByDescending(a => a.UpdatedAt),
                "alphabetical" => query.OrderBy(a => a.FullName),
                _ => query.OrderByDescending(a => a.IsProfileComplete) // relevance (default)
                    .ThenByDescending(a => a.Skills.Count)
                    .ThenByDescending(a => a.TotalExperienceYears)
            };

            // Apply pagination
            Applicants = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }
    }
}
