using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using RESUMATE_FINAL_WORKING_MODEL.Services;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter.Applicants
{
    [Authorize(Roles = "Recruiter")]
    public class DetailModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public DetailModel(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public Models.Recruiter? CurrentRecruiter { get; set; }
        public Application? Application { get; set; }
        public List<RecruiterNote> Notes { get; set; } = new();
        public List<Interview> Interviews { get; set; } = new();

        [BindProperty]
        public NoteInputModel NoteInput { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
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

            // Get application with all related data
            Application = await _context.Applications
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap!.ApplicantSkills)
                        .ThenInclude(s => s.Skill)
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap!.Experiences)
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap!.Educations)
                .Include(a => a.Job)
                    .ThenInclude(j => j!.Company)
                .Include(a => a.ReviewedBy)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (Application == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToPage("/Recruiter/Applicants/Browse");
            }

            // Verify recruiter has access to this application
            if (Application.Job?.CompanyId != CurrentRecruiter.CompanyId)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToPage("/Recruiter/Applicants/Browse");
            }

            // Load notes
            Notes = await _context.RecruiterNotes
                .Include(n => n.Recruiter)
                .Where(n => n.ApplicationId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Load interviews
            Interviews = await _context.Interviews
                .Include(i => i.Recruiter)
                .Where(i => i.ApplicationId == id)
                .OrderByDescending(i => i.ScheduledDateTime)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int id, string newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (recruiter == null)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToPage();
            }

            var application = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.Applicant)
                .FirstOrDefaultAsync(a => a.Id == id && a.Job!.CompanyId == recruiter.CompanyId);

            if (application == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToPage();
            }

            application.Status = newStatus;
            application.StatusChangedAt = DateTime.UtcNow;
            application.ReviewedByRecruiterId = recruiter.Id;
            application.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send email notification
            try
            {
                await _emailService.SendApplicationStatusChangeAsync(application, newStatus);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }

            TempData["Success"] = $"Status changed to {newStatus}";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddNoteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (recruiter == null)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToPage();
            }

            if (!ModelState.IsValid)
            {
                return await OnGetAsync(id);
            }

            var note = new RecruiterNote
            {
                ApplicationId = id,
                RecruiterId = recruiter.Id,
                NoteText = NoteInput.NoteText,
                IsImportant = NoteInput.IsImportant,
                CreatedAt = DateTime.UtcNow
            };

            _context.RecruiterNotes.Add(note);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Note added successfully";
            return RedirectToPage(new { id });
        }

        public class NoteInputModel
        {
            [Required(ErrorMessage = "Note text is required")]
            [StringLength(5000, MinimumLength = 1)]
            public string NoteText { get; set; } = string.Empty;

            public bool IsImportant { get; set; }
        }
    }
}
