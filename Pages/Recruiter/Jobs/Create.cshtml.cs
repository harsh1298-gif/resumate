using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter.Jobs
{
    [Authorize(Roles = "Recruiter")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public JobInputModel Input { get; set; } = new();

        public Models.Recruiter? CurrentRecruiter { get; set; }
        public List<SelectListItem> SkillOptions { get; set; } = new();

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

            // Load available skills for dropdown
            SkillOptions = await _context.Skills
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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

            if (!ModelState.IsValid)
            {
                // Reload skill options on validation failure
                SkillOptions = await _context.Skills
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Name
                    })
                    .ToListAsync();

                return Page();
            }

            // Custom validation
            if (Input.SalaryMin.HasValue && Input.SalaryMax.HasValue && Input.SalaryMin >= Input.SalaryMax)
            {
                ModelState.AddModelError("Input.SalaryMax", "Maximum salary must be greater than minimum salary.");
                SkillOptions = await _context.Skills.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToListAsync();
                return Page();
            }

            if (Input.ApplicationDeadline.HasValue && Input.ApplicationDeadline <= DateTime.Today)
            {
                ModelState.AddModelError("Input.ApplicationDeadline", "Application deadline must be in the future.");
                SkillOptions = await _context.Skills.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToListAsync();
                return Page();
            }

            if (Input.SelectedSkillIds == null || !Input.SelectedSkillIds.Any())
            {
                ModelState.AddModelError("Input.SelectedSkillIds", "Please select at least one required skill.");
                SkillOptions = await _context.Skills.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToListAsync();
                return Page();
            }

            // Create new job
            var job = new Job
            {
                Title = Input.Title,
                Description = Input.Description,
                Location = Input.Location,
                ExperienceLevel = Input.ExperienceLevel,
                Type = Input.Type,
                Category = Input.Category,
                Salary = Input.SalaryMin, // Store min as main salary for backward compatibility
                CompanyId = CurrentRecruiter.CompanyId,
                RecruiterId = CurrentRecruiter.Id,
                Benefits = Input.Benefits,
                NumberOfOpenings = Input.NumberOfOpenings,
                RemoteWorkOption = Input.RemoteWorkOption,
                RemoteWorkType = Input.RemoteWorkType,
                ApplicationDeadline = Input.ApplicationDeadline,
                KeyResponsibilities = Input.KeyResponsibilities,
                PreferredQualifications = Input.PreferredQualifications,
                IsActive = Input.PublishImmediately,
                PostedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            // Add required skills
            if (Input.SelectedSkillIds != null && Input.SelectedSkillIds.Any())
            {
                foreach (var skillId in Input.SelectedSkillIds)
                {
                    var jobRequirement = new JobRequirement
                    {
                        JobId = job.Id,
                        SkillId = skillId
                    };
                    _context.JobRequirements.Add(jobRequirement);
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = Input.PublishImmediately
                ? "Job posted successfully!"
                : "Job saved as draft. You can publish it later from Active Jobs.";

            return RedirectToPage("/RecruiterDashboard");
        }

        public class JobInputModel
        {
            [Required(ErrorMessage = "Job title is required")]
            [StringLength(200, MinimumLength = 3, ErrorMessage = "Job title must be between 3 and 200 characters")]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "Job description is required")]
            [StringLength(10000, MinimumLength = 100, ErrorMessage = "Job description must be at least 100 characters")]
            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "Location is required")]
            [StringLength(200)]
            public string Location { get; set; } = string.Empty;

            [Required(ErrorMessage = "Experience level is required")]
            public ExperienceLevel ExperienceLevel { get; set; }

            [Required(ErrorMessage = "Job type is required")]
            public JobType Type { get; set; }

            [Required(ErrorMessage = "Job category is required")]
            public JobCategory Category { get; set; }

            [Range(0, 10000000, ErrorMessage = "Minimum salary must be between 0 and 10,000,000")]
            public decimal? SalaryMin { get; set; }

            [Range(0, 10000000, ErrorMessage = "Maximum salary must be between 0 and 10,000,000")]
            public decimal? SalaryMax { get; set; }

            public bool HideSalary { get; set; }

            [StringLength(2000)]
            public string? Benefits { get; set; }

            [Range(1, 999, ErrorMessage = "Number of openings must be between 1 and 999")]
            public int NumberOfOpenings { get; set; } = 1;

            public bool RemoteWorkOption { get; set; }

            [StringLength(50)]
            public string? RemoteWorkType { get; set; } // Remote, Hybrid, OnSite

            public DateTime? ApplicationDeadline { get; set; }

            [StringLength(5000)]
            public string? KeyResponsibilities { get; set; }

            [StringLength(2000)]
            public string? PreferredQualifications { get; set; }

            public List<int> SelectedSkillIds { get; set; } = new();

            public bool PublishImmediately { get; set; } = true;
        }
    }
}
