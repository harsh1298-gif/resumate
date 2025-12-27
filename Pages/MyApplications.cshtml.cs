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
    public class MyApplicationsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyApplicationsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "Recent";

        public List<ApplicationViewModel> Applications { get; set; } = new List<ApplicationViewModel>();
        public ApplicationStatistics Statistics { get; set; } = new ApplicationStatistics();

        public int TotalApplications => Applications.Count;
        public bool HasApplications => Applications.Any();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get current user's applicant profile
            var user = await _userManager.GetUserAsync(User);
            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (applicant == null)
            {
                TempData["Error"] = "Please complete your profile first.";
                return RedirectToPage("/EditProfile");
            }

            // Get all applications for this applicant
            var applicationsQuery = _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company)
                .Where(a => a.ApplicantId == applicant.Id)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            {
                applicationsQuery = applicationsQuery.Where(a => a.Status == StatusFilter);
            }

            // Get applications
            var applications = await applicationsQuery.ToListAsync();

            // Sort applications
            applications = SortBy switch
            {
                "Recent" => applications.OrderByDescending(a => a.ApplicationDate).ToList(),
                "Oldest" => applications.OrderBy(a => a.ApplicationDate).ToList(),
                "Company" => applications.OrderBy(a => a.Job?.Company?.Name).ToList(),
                "Status" => applications.OrderBy(a => a.Status).ToList(),
                _ => applications.OrderByDescending(a => a.ApplicationDate).ToList()
            };

            // Map to view models
            Applications = applications.Select(a => new ApplicationViewModel
            {
                Id = a.Id,
                JobId = a.JobId,
                JobTitle = a.Job?.Title ?? "Unknown Position",
                CompanyName = a.Job?.Company?.Name ?? "Unknown Company",
                CompanyLogo = a.Job?.Company?.LogoPath ?? "/images/default-company.png",
                Location = a.Job?.Location ?? "Not specified",
                JobType = a.Job?.Type.ToString() ?? "Not specified",
                Salary = a.Job?.Salary,
                Status = a.Status ?? "Pending",
                ApplicationDate = a.ApplicationDate,
                IsJobActive = a.Job?.IsActive ?? false
            }).ToList();

            // Calculate statistics
            Statistics = new ApplicationStatistics
            {
                TotalApplications = applications.Count,
                PendingCount = applications.Count(a => a.Status == "Pending" || string.IsNullOrEmpty(a.Status)),
                UnderReviewCount = applications.Count(a => a.Status == "Under Review"),
                ShortlistedCount = applications.Count(a => a.Status == "Shortlisted"),
                InterviewCount = applications.Count(a => a.Status == "Interview"),
                RejectedCount = applications.Count(a => a.Status == "Rejected"),
                AcceptedCount = applications.Count(a => a.Status == "Accepted" || a.Status == "Hired")
            };

            return Page();
        }

        public async Task<IActionResult> OnPostWithdrawAsync(int applicationId)
        {
            var application = await _context.Applications
                .Include(a => a.Applicant)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToPage();
            }

            // Verify ownership
            var user = await _userManager.GetUserAsync(User);
            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (applicant == null || application.ApplicantId != applicant.Id)
            {
                TempData["Error"] = "Unauthorized action.";
                return RedirectToPage();
            }

            // Update status to Withdrawn
            application.Status = "Withdrawn";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application withdrawn successfully.";
            return RedirectToPage();
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
                return "Not disclosed";

            if (salary >= 100000)
                return $"₹{salary / 100000:F1}L/year";
            else if (salary >= 1000)
                return $"₹{salary / 1000:F0}K/year";
            else
                return $"₹{salary:N0}/year";
        }

        public string GetStatusColor(string status)
        {
            return status switch
            {
                "Pending" => "bg-yellow-100 text-yellow-800",
                "Under Review" => "bg-blue-100 text-blue-800",
                "Shortlisted" => "bg-purple-100 text-purple-800",
                "Interview" => "bg-indigo-100 text-indigo-800",
                "Accepted" => "bg-green-100 text-green-800",
                "Hired" => "bg-green-100 text-green-800",
                "Rejected" => "bg-red-100 text-red-800",
                "Withdrawn" => "bg-gray-100 text-gray-800",
                _ => "bg-gray-100 text-gray-800"
            };
        }

        public string GetStatusIcon(string status)
        {
            return status switch
            {
                "Pending" => "fas fa-clock",
                "Under Review" => "fas fa-search",
                "Shortlisted" => "fas fa-star",
                "Interview" => "fas fa-calendar-check",
                "Accepted" => "fas fa-check-circle",
                "Hired" => "fas fa-briefcase",
                "Rejected" => "fas fa-times-circle",
                "Withdrawn" => "fas fa-ban",
                _ => "fas fa-question-circle"
            };
        }
    }

    public class ApplicationViewModel
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; }
        public bool IsJobActive { get; set; }
    }

    public class ApplicationStatistics
    {
        public int TotalApplications { get; set; }
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ShortlistedCount { get; set; }
        public int InterviewCount { get; set; }
        public int RejectedCount { get; set; }
        public int AcceptedCount { get; set; }
    }
}
