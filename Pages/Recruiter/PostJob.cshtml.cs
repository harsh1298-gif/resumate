using System.ComponentModel.DataAnnotations;
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
    public class PostJobModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PostJobModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        [Required(ErrorMessage = "Job title is required")]
        [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters")]
        public string JobTitle { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Description is required")]
        [MinLength(50, ErrorMessage = "Description must be at least 50 characters")]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Job type is required")]
        public string JobType { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Experience level is required")]
        public string ExperienceLevel { get; set; } = string.Empty;

        [BindProperty]
        [Range(0, 10000000, ErrorMessage = "Salary must be a valid amount")]
        public decimal? Salary { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "At least one skill is required")]
        public string SkillsInput { get; set; } = string.Empty;

        [BindProperty]
        public DateTime? ClosingDate { get; set; }

        [BindProperty]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ApplicationLink { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            // Verify recruiter exists
            var recruiter = await _context.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.Email == user.Email);

            if (recruiter == null)
            {
                TempData["Error"] = "Recruiter profile not found.";
                return RedirectToPage("/Index");
            }

            if (recruiter.Company == null)
            {
                TempData["Error"] = "You must be associated with a company to post jobs.";
                return RedirectToPage("/Recruiter/Dashboard");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            // Get recruiter
            var recruiter = await _context.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.Email == user.Email);

            if (recruiter == null)
            {
                TempData["Error"] = "Recruiter profile not found.";
                return RedirectToPage("/Index");
            }

            if (recruiter.Company == null)
            {
                TempData["Error"] = "You must be associated with a company to post jobs.";
                return RedirectToPage("/Recruiter/Dashboard");
            }

            // Parse and validate enums
            if (!Enum.TryParse<Models.JobType>(JobType, out var jobTypeEnum))
            {
                ModelState.AddModelError(nameof(JobType), "Invalid job type selected");
                return Page();
            }

            if (!Enum.TryParse<JobCategory>(Category, out var categoryEnum))
            {
                ModelState.AddModelError(nameof(Category), "Invalid category selected");
                return Page();
            }

            if (!Enum.TryParse<Models.ExperienceLevel>(ExperienceLevel, out var experienceEnum))
            {
                ModelState.AddModelError(nameof(ExperienceLevel), "Invalid experience level selected");
                return Page();
            }

            // Validate closing date
            if (ClosingDate.HasValue && ClosingDate.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(ClosingDate), "Closing date must be in the future");
                return Page();
            }

            // Create job
            var job = new Job
            {
                Title = JobTitle,
                Description = Description,
                Location = Location,
                Type = jobTypeEnum,
                Category = categoryEnum,
                ExperienceLevel = experienceEnum,
                Salary = Salary,
                CompanyId = recruiter.Company.Id,
                RecruiterId = recruiter.Id,
                ClosingDate = ClosingDate,
                ApplicationLink = ApplicationLink,
                PostedDate = DateTime.UtcNow,
                IsActive = action == "post", // Active if posting, inactive if draft
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync(); // Save to get job ID

            // Process skills
            var skillNames = SkillsInput
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (skillNames.Any())
            {
                foreach (var skillName in skillNames)
                {
                    // Check if skill exists
                    var skill = await _context.Skills
                        .FirstOrDefaultAsync(s => s.Name!.ToLower() == skillName.ToLower());

                    // Create skill if it doesn't exist
                    if (skill == null)
                    {
                        skill = new Skill { Name = skillName };
                        _context.Skills.Add(skill);
                        await _context.SaveChangesAsync(); // Save to get skill ID
                    }

                    // Create job requirement
                    var jobRequirement = new JobRequirement
                    {
                        JobId = job.Id,
                        SkillId = skill.Id
                    };

                    _context.JobRequirements.Add(jobRequirement);
                }

                await _context.SaveChangesAsync();
            }

            // Set success message based on action
            if (action == "post")
            {
                TempData["Success"] = "Job posted successfully!";
            }
            else
            {
                TempData["Success"] = "Job saved as draft successfully!";
            }

            return RedirectToPage("/Recruiter/Dashboard");
        }
    }
}
