using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using RESUMATE_FINAL_WORKING_MODEL.Services;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages.Recruiter.Applicants
{
    [Authorize(Roles = "Recruiter")]
    public class BrowseModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public BrowseModel(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public Models.Recruiter? CurrentRecruiter { get; set; }
        public List<ApplicationViewModel> Applications { get; set; } = new();
        public List<SelectListItem> JobOptions { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        // Filter Parameters
        [BindProperty(SupportsGet = true)]
        public int? JobId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;

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

            // Load job options for filter
            JobOptions = await _context.Jobs
                .Where(j => j.CompanyId == CurrentRecruiter.CompanyId)
                .Select(j => new SelectListItem
                {
                    Value = j.Id.ToString(),
                    Text = j.Title
                })
                .ToListAsync();

            // Build query
            var query = _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Job)
                .Where(a => a.Job!.CompanyId == CurrentRecruiter.CompanyId);

            // Apply filters
            if (JobId.HasValue)
                query = query.Where(a => a.JobId == JobId.Value);

            if (!string.IsNullOrEmpty(Status))
                query = query.Where(a => a.Status == Status);

            if (FromDate.HasValue)
                query = query.Where(a => a.ApplicationDate >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(a => a.ApplicationDate <= ToDate.Value);

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(a =>
                    a.Applicant!.FullName.Contains(SearchTerm) ||
                    a.Applicant.Email.Contains(SearchTerm));
            }

            // Get total count before pagination
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // Apply sorting
            query = SortBy switch
            {
                "Name" => query.OrderBy(a => a.Applicant!.FullName),
                "Job" => query.OrderBy(a => a.Job!.Title),
                "Date" => query.OrderByDescending(a => a.ApplicationDate),
                "Status" => query.OrderBy(a => a.Status),
                _ => query.OrderByDescending(a => a.ApplicationDate)
            };

            // Apply pagination
            var applications = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Map to view model
            Applications = applications.Select(a => new ApplicationViewModel
            {
                ApplicationId = a.Id,
                ApplicantId = a.ApplicantId,
                ApplicantName = a.Applicant?.FullName ?? "Unknown",
                ApplicantEmail = a.Applicant?.Email ?? "",
                ApplicantPhone = a.Applicant?.PhoneNumber ?? "",
                ProfilePhoto = a.Applicant?.ProfilePhotoPath ?? "/images/default-avatar.png",
                JobTitle = a.Job?.Title ?? "",
                JobId = a.JobId,
                ApplicationDate = a.ApplicationDate,
                Status = a.Status ?? "Pending",
                ResumeFilePath = a.Applicant?.ResumeFilePath
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int applicationId, string newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (recruiter == null)
            {
                return new JsonResult(new { success = false, message = "Unauthorized" });
            }

            var application = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.Applicant)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job!.CompanyId == recruiter.CompanyId);

            if (application == null)
            {
                return new JsonResult(new { success = false, message = "Application not found" });
            }

            var oldStatus = application.Status;
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
                // Log error but don't fail the request
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }

            return new JsonResult(new { success = true, message = $"Status changed from {oldStatus} to {newStatus}" });
        }

        public async Task<IActionResult> OnPostBulkChangeStatusAsync(List<int> applicationIds, string newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user!.Id);

            if (recruiter == null)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToPage();
            }

            var applications = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.Applicant)
                .Where(a => applicationIds.Contains(a.Id) && a.Job!.CompanyId == recruiter.CompanyId)
                .ToListAsync();

            foreach (var application in applications)
            {
                application.Status = newStatus;
                application.StatusChangedAt = DateTime.UtcNow;
                application.ReviewedByRecruiterId = recruiter.Id;
                application.ReviewedAt = DateTime.UtcNow;

                // Send email notification
                try
                {
                    await _emailService.SendApplicationStatusChangeAsync(application, newStatus);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{applications.Count} application(s) updated to {newStatus}";
            return RedirectToPage();
        }

        public class ApplicationViewModel
        {
            public int ApplicationId { get; set; }
            public int ApplicantId { get; set; }
            public string ApplicantName { get; set; } = string.Empty;
            public string ApplicantEmail { get; set; } = string.Empty;
            public string ApplicantPhone { get; set; } = string.Empty;
            public string ProfilePhoto { get; set; } = string.Empty;
            public string JobTitle { get; set; } = string.Empty;
            public int JobId { get; set; }
            public DateTime ApplicationDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? ResumeFilePath { get; set; }
        }
    }
}
