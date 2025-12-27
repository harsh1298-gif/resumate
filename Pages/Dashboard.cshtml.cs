using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System.Security.Claims;
using ApplicantModel = RESUMATE_FINAL_WORKING_MODEL.Models.Applicant;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    [Authorize(Roles = "Applicant")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(AppDbContext context, ILogger<DashboardModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public ApplicantModel? Applicant { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool IsProfileComplete { get; set; }
        public int ProfileCompletion { get; set; }
        public int ApplicationsCount { get; set; }
        public int ProfileViews { get; set; }
        public int MatchScore { get; set; }
        public int ActiveJobsCount { get; set; }
        public int SavedJobsCount { get; set; }
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
        public List<JobMatch> JobMatches { get; set; } = new List<JobMatch>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Login");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    UserName = user.UserName ?? "User";
                    UserEmail = user.Email ?? "No email provided";
                }

                try
                {
                    Applicant = await _context.Applicants
                        .Include(a => a.ApplicantSkills)
                            .ThenInclude(appSkill => appSkill.Skill)
                        .Include(a => a.Experiences)
                        .Include(a => a.Educations)
                        .FirstOrDefaultAsync(a => a.UserId == userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading applicant profile");
                    Applicant = null;
                }

                ProfileCompletion = CalculateProfileCompletion(Applicant);
                IsProfileComplete = ProfileCompletion >= 80;
                ApplicationsCount = await GetApplicationsCount(Applicant?.Id ?? 0);
                ProfileViews = await GetProfileViews(Applicant?.Id ?? 0);
                MatchScore = CalculateMatchScore(Applicant);
                ActiveJobsCount = await GetActiveJobsCount();
                SavedJobsCount = await GetSavedJobsCount(userId);
                RecentActivities = await GetRecentActivities(userId);
                JobMatches = await GetJobMatches(Applicant);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "An error occurred while loading the dashboard.";
                return RedirectToPage("/Error");
            }
        }

        private int CalculateProfileCompletion(ApplicantModel? applicant)
        {
            if (applicant == null) return 0;

            int completion = 0;

            if (!string.IsNullOrEmpty(applicant.FullName)) completion += 5;
            if (!string.IsNullOrEmpty(applicant.Email)) completion += 5;
            if (!string.IsNullOrEmpty(applicant.PhoneNumber)) completion += 5;
            if (!string.IsNullOrEmpty(applicant.Address)) completion += 5;
            if (!string.IsNullOrEmpty(applicant.City)) completion += 5;
            if (!string.IsNullOrEmpty(applicant.Pincode)) completion += 5;
            if (!string.IsNullOrEmpty(applicant.ProfessionalSummary)) completion += 10;
            if (!string.IsNullOrEmpty(applicant.Objective)) completion += 10;
            if (applicant.HasResume) completion += 10;
            if (applicant.ApplicantSkills?.Any() == true) completion += 10;
            if (applicant.Experiences?.Any() == true) completion += 15;
            if (applicant.Educations?.Any() == true) completion += 15;

            return completion;
        }

        private int CalculateMatchScore(ApplicantModel? applicant)
        {
            if (applicant == null) return 0;

            int score = 50;
            score += Math.Min(ProfileCompletion / 2, 25);

            if (applicant.ApplicantSkills?.Any() == true)
                score += Math.Min(applicant.ApplicantSkills.Count * 2, 15);

            if (applicant.Experiences?.Any() == true)
                score += Math.Min(applicant.Experiences.Count * 3, 10);

            return Math.Min(score, 100);
        }

        private async Task<int> GetApplicationsCount(int applicantId)
        {
            if (applicantId <= 0) return 0;

            try
            {
                return await _context.Applications.CountAsync(a => a.ApplicantId == applicantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications count");
                return 0;
            }
        }

        private async Task<int> GetProfileViews(int applicantId)
        {
            if (applicantId <= 0) return 0;
            return await Task.FromResult(applicantId * 5);
        }

        private async Task<int> GetActiveJobsCount()
        {
            try
            {
                return await _context.Jobs.CountAsync(j => j.IsActive);
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetSavedJobsCount(string userId)
        {
            return await Task.FromResult(0);
        }

        private async Task<List<RecentActivity>> GetRecentActivities(string userId)
        {
            var activities = new List<RecentActivity>();

            if (Applicant != null)
            {
                try
                {
                    var recentApplications = await _context.Applications
                        .Include(a => a.Job)
                            .ThenInclude(j => j!.Company)
                        .Where(a => a.ApplicantId == Applicant.Id)
                        .OrderByDescending(a => a.ApplicationDate)
                        .Take(3)
                        .ToListAsync();

                    foreach (var application in recentApplications)
                    {
                        activities.Add(new RecentActivity
                        {
                            Title = "Job Application Submitted",
                            Description = string.Format("Applied for {0}", application.Job?.Title ?? "a position"),
                            Timestamp = application.ApplicationDate,
                            Icon = "fas fa-paper-plane",
                            Color = "blue"
                        });
                    }

                    if (Applicant.UpdatedAt != default && Applicant.UpdatedAt > DateTime.UtcNow.AddDays(-7))
                    {
                        activities.Add(new RecentActivity
                        {
                            Title = "Profile Updated",
                            Description = "Your profile was updated",
                            Timestamp = Applicant.UpdatedAt,
                            Icon = "fas fa-user-edit",
                            Color = "green"
                        });
                    }

                    if (Applicant.ResumeUploadDate.HasValue && Applicant.ResumeUploadDate.Value > DateTime.UtcNow.AddDays(-7))
                    {
                        activities.Add(new RecentActivity
                        {
                            Title = "Resume Uploaded",
                            Description = "New PDF resume uploaded",
                            Timestamp = Applicant.ResumeUploadDate.Value,
                            Icon = "fas fa-file-pdf",
                            Color = "red"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading activities");
                }
            }

            if (!activities.Any())
            {
                activities.Add(new RecentActivity
                {
                    Title = "Welcome!",
                    Description = "Complete your profile to get started",
                    Timestamp = DateTime.UtcNow,
                    Icon = "fas fa-rocket",
                    Color = "purple"
                });
            }

            return activities.OrderByDescending(a => a.Timestamp).Take(3).ToList();
        }

        private async Task<List<JobMatch>> GetJobMatches(ApplicantModel? applicant)
        {
            var jobMatches = new List<JobMatch>();
            if (applicant == null) return jobMatches;

            try
            {
                var activeJobs = await _context.Jobs
                    .Include(j => j.Company)
                    .Include(j => j.RequiredSkills)
                        .ThenInclude(js => js.Skill)
                    .Where(j => j.IsActive)
                    .OrderByDescending(j => j.PostedDate)
                    .Take(6)
                    .ToListAsync();

                foreach (var job in activeJobs)
                {
                    var matchPercentage = CalculateJobMatchPercentage(applicant, job);
                    if (matchPercentage > 30)
                    {
                        jobMatches.Add(new JobMatch
                        {
                            JobId = job.Id,
                            JobTitle = job.Title ?? "Unknown",
                            CompanyName = job.Company?.Name ?? "Unknown",
                            Location = job.Location ?? "Remote",
                            Salary = job.Salary,
                            MatchPercentage = matchPercentage,
                            PostedDate = job.PostedDate,
                            JobType = job.Type.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading job matches");
            }

            return jobMatches.OrderByDescending(jm => jm.MatchPercentage).ToList();
        }

        private int CalculateJobMatchPercentage(ApplicantModel applicant, Job job)
        {
            int matchScore = 0;

            try
            {
                var applicantSkills = applicant.ApplicantSkills?
                    .Where(appSkill => appSkill.Skill != null && !string.IsNullOrEmpty(appSkill.Skill.Name))
                    .Select(appSkill => appSkill.Skill!.Name!.ToLower())
                    .ToList() ?? new List<string>();

                if (!string.IsNullOrEmpty(applicant.City) && !string.IsNullOrEmpty(job.Location))
                {
                    if (job.Location.ToLower().Contains(applicant.City.ToLower()))
                        matchScore += 25;
                    else if (job.Location.ToLower().Contains("remote"))
                        matchScore += 20;
                }

                if (applicantSkills.Any() && job.RequiredSkills?.Any() == true)
                {
                    var jobSkillNames = job.RequiredSkills
                        .Where(js => js.Skill != null && !string.IsNullOrEmpty(js.Skill.Name))
                        .Select(js => js.Skill!.Name!.ToLower())
                        .ToList();

                    var matchingSkills = applicantSkills.Count(skill =>
                        jobSkillNames.Any(jobSkill => jobSkill.Contains(skill)));

                    if (matchingSkills > 0)
                        matchScore += Math.Min(matchingSkills * 10, 40);
                }

                if (applicant.Experiences?.Any() == true) matchScore += 20;
                if (applicant.Educations?.Any() == true) matchScore += 15;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating match");
                return 0;
            }

            return Math.Min(matchScore, 100);
        }

        public string GetFormattedPhone(string? phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return "Not provided";
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return string.Format("+91 {0} {1}", digits.Substring(0, 5), digits.Substring(5));
            return phoneNumber;
        }

        public string GetRelativeTime(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return string.Format("{0}m ago", (int)timeSpan.TotalMinutes);
            if (timeSpan.TotalHours < 24) return string.Format("{0}h ago", (int)timeSpan.TotalHours);
            if (timeSpan.TotalDays < 7) return string.Format("{0}d ago", (int)timeSpan.TotalDays);
            if (timeSpan.TotalDays < 30) return string.Format("{0}w ago", (int)(timeSpan.TotalDays / 7));
            return timestamp.ToString("MMM d, yyyy");
        }

        public string GetFormattedSalary(decimal? salary)
        {
            if (!salary.HasValue) return "Not specified";
            if (salary.Value >= 100000) return string.Format("Rs.{0:F1}L/year", salary.Value / 100000);
            if (salary.Value >= 1000) return string.Format("Rs.{0:F0}K/year", salary.Value / 1000);
            return string.Format("Rs.{0:N0}/year", salary.Value);
        }
    }

    public class RecentActivity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class JobMatch
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public int MatchPercentage { get; set; }
        public DateTime PostedDate { get; set; }
        public string JobType { get; set; } = string.Empty;
    }
}