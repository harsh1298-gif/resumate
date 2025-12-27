namespace RESUMATE_FINAL_WORKING_MODEL.Models.ViewModels
{
    public class RecruiterDashboardViewModel
    {
        public string RecruiterName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string RecruiterEmail { get; set; } = string.Empty;
        public DashboardStatistics Statistics { get; set; } = new DashboardStatistics();
        public List<RecentApplicantViewModel> RecentApplicants { get; set; } = new List<RecentApplicantViewModel>();
        public List<ActiveJobViewModel> ActiveJobs { get; set; } = new List<ActiveJobViewModel>();
    }

    public class DashboardStatistics
    {
        public int ActiveJobsCount { get; set; }
        public int TotalApplicantsCount { get; set; }
        public int ScheduledInterviewsCount { get; set; }
        public int HiredCount { get; set; }
        public int PendingReviewCount { get; set; }
        public int ShortlistedCount { get; set; }
    }

    public class RecentApplicantViewModel
    {
        public int ApplicationId { get; set; }
        public int ApplicantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string ProfilePhoto { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AppliedDate { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal? MatchScore { get; set; }
    }

    public class ActiveJobViewModel
    {
        public int JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public int ApplicantCount { get; set; }
        public int NewApplicantCount { get; set; }
        public string PostedDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }
}