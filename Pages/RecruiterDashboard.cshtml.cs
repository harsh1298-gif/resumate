using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using RESUMATE_FINAL_WORKING_MODEL.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    [Authorize(Roles = "Recruiter")]
    public class RecruiterDashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RecruiterDashboardModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public RecruiterDashboardViewModel Dashboard { get; set; } = new RecruiterDashboardViewModel();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            var isRecruiter = await _userManager.IsInRoleAsync(user, "Recruiter");
            if (!isRecruiter)
                return RedirectToPage("/ApplicantDashboard");

            var recruiter = await _context.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                // Recruiter profile doesn't exist - show welcome message
                Dashboard.RecruiterName = user.UserName ?? "Recruiter";
                Dashboard.RecruiterEmail = user.Email ?? string.Empty;
                Dashboard.CompanyName = "No Company";
                TempData["Warning"] = "Your recruiter profile is incomplete. Please contact the administrator to complete your profile setup.";
                return Page();
            }

            await BuildDashboardAsync(recruiter, user);
            return Page();
        }

        private async Task BuildDashboardAsync(Models.Recruiter recruiter, IdentityUser user)
        {
            Dashboard.RecruiterName = recruiter.Name;
            Dashboard.CompanyName = recruiter.Company?.Name ?? "Your Company";
            Dashboard.RecruiterEmail = user.Email ?? string.Empty;

            var jobs = await _context.Jobs
                .Where(j => j.CompanyId == recruiter.CompanyId)
                .ToListAsync();

            var jobIds = jobs.Select(j => j.Id).ToList();

            var applications = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Job)
                .Where(a => jobIds.Contains(a.JobId))
                .ToListAsync();

            // Statistics
            Dashboard.Statistics.ActiveJobsCount = jobs.Count;
            Dashboard.Statistics.TotalApplicantsCount = applications.Count;
            Dashboard.Statistics.PendingReviewCount = applications.Count(a =>
                a.Status == "Pending" || a.Status == "Under Review" || string.IsNullOrEmpty(a.Status));
            Dashboard.Statistics.ShortlistedCount = applications.Count(a => a.Status == "Shortlisted");
            Dashboard.Statistics.HiredCount = applications.Count(a => a.Status == "Hired");

            // Count scheduled interviews
            Dashboard.Statistics.ScheduledInterviewsCount = await _context.Interviews
                .Where(i => i.RecruiterId == recruiter.Id && i.Status == InterviewStatus.Scheduled)
                .CountAsync();

            // Recent applicants
            Dashboard.RecentApplicants = applications
                .OrderByDescending(a => a.ApplicationDate).Take(10)
                .Select(a => new RecentApplicantViewModel
                {
                    ApplicationId = a.Id,
                    ApplicantId = a.ApplicantId,
                    Name = a.Applicant?.FullName ?? "Unknown",
                    JobTitle = a.Job?.Title ?? "Position",
                    ProfilePhoto = a.Applicant?.ProfilePhotoPath ?? "/images/default-avatar.png",
                    Status = a.Status ?? "Pending",
                    AppliedDate = a.ApplicationDate.ToString("MMM dd, yyyy"),
                    Email = a.Applicant?.Email ?? string.Empty
                })
                .ToList();

            // Active jobs
            // In the BuildDashboardAsync method, update the ActiveJobs section:
            Dashboard.ActiveJobs = jobs
                .Select(j => new ActiveJobViewModel
                {
                    JobId = j.Id,
                    Title = j.Title,
                    Location = j.Location ?? "Remote",
                    Department = j.Category.ToString(), // Use Category enum
                    JobType = j.Type.ToString(),        // Use Type enum
                    ApplicantCount = applications.Count(a => a.JobId == j.Id),
                    NewApplicantCount = applications.Count(a => a.JobId == j.Id &&
                        (a.Status == "Pending" || string.IsNullOrEmpty(a.Status))),
                    PostedDate = j.PostedDate.ToString("MMM dd, yyyy"),
                    Status = "Active"
                })
                .OrderByDescending(j => j.NewApplicantCount)
                .ToList();
        }
    }
}