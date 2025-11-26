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
    public class EditJobModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EditJobModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public int JobId { get; set; }

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

        [BindProperty]
        public bool IsActive { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            // Get job with required skills
            var job = await _context.Jobs
                .Include(j => j.RequiredSkills)
                    .ThenInclude(rs => rs.Skill)
                .FirstOrDefaultAsync(j => j.Id == id.Value);

            if (job == null)
            {
                return NotFound();
            }

            // Verify recruiter owns this job
            if (job.RecruiterId != recruiter.Id)
            {
                TempData["Error"] = "You don't have permission to edit this job.";
                return RedirectToPage("/Recruiter/Dashboard");
            }

            // Populate form
            JobId = job.Id;
            JobTitle = job.Title;
            Description = job.Description;
            Location = job.Location;
            JobType = job.Type.ToString();
            Category = job.Category.ToString();
            ExperienceLevel = job.ExperienceLevel.ToString();
            Salary = job.Salary;
            ClosingDate = job.ClosingDate;
            ApplicationLink = job.ApplicationLink;
            IsActive = job.IsActive;

            // Get skills as comma-separated string
            SkillsInput = string.Join(", ", job.RequiredSkills.Select(rs => rs.Skill!.Name));

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.Email == user.Email);

            if (recruiter == null)
            {
                return RedirectToPage("/Index");
            }

            // Get existing job
            var job = await _context.Jobs
                .Include(j => j.RequiredSkills)
                .FirstOrDefaultAsync(j => j.Id == JobId);

            if (job == null)
            {
                return NotFound();
            }

            // Verify ownership
            if (job.RecruiterId != recruiter.Id)
            {
                TempData["Error"] = "You don't have permission to edit this job.";
                return RedirectToPage("/Recruiter/Dashboard");
            }

            // Parse enums
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

            // Update job
            job.Title = JobTitle;
            job.Description = Description;
            job.Location = Location;
            job.Type = jobTypeEnum;
            job.Category = categoryEnum;
            job.ExperienceLevel = experienceEnum;
            job.Salary = Salary;
            job.ClosingDate = ClosingDate;
            job.ApplicationLink = ApplicationLink;
            job.IsActive = IsActive;
            job.UpdatedAt = DateTime.UtcNow;

            // Remove old skills
            _context.JobRequirements.RemoveRange(job.RequiredSkills);

            // Add new skills
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
                    var skill = await _context.Skills
                        .FirstOrDefaultAsync(s => s.Name!.ToLower() == skillName.ToLower());

                    if (skill == null)
                    {
                        skill = new Skill { Name = skillName };
                        _context.Skills.Add(skill);
                        await _context.SaveChangesAsync();
                    }

                    var jobRequirement = new JobRequirement
                    {
                        JobId = job.Id,
                        SkillId = skill.Id
                    };

                    _context.JobRequirements.Add(jobRequirement);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Job updated successfully!";
            return RedirectToPage("/Recruiter/Dashboard");
        }

        public async Task<IActionResult> OnPostDeleteAsync()
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

            var job = await _context.Jobs
                .Include(j => j.RequiredSkills)
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == JobId);

            if (job == null)
            {
                return NotFound();
            }

            // Verify ownership
            if (job.RecruiterId != recruiter.Id)
            {
                TempData["Error"] = "You don't have permission to delete this job.";
                return RedirectToPage("/Recruiter/Dashboard");
            }

            // Remove related data
            _context.JobRequirements.RemoveRange(job.RequiredSkills);
            _context.Applications.RemoveRange(job.Applications);
            _context.Jobs.Remove(job);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Job deleted successfully!";
            return RedirectToPage("/Recruiter/Dashboard");
        }
    }
}
