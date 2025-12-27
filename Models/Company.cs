using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Company
    {
        public int Id { get; set; }

        // Basic Information
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = null!;

        [Required]
        public Industry Industry { get; set; }

        [Required]
        public CompanySize CompanySize { get; set; }

        public int? EmployeeCount { get; set; }

        public int? FoundedYear { get; set; }

        // Contact Information
        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; } = null!;

        [Phone]
        public string? ContactPhone { get; set; }

        [Url]
        public string? Website { get; set; }

        // Address Information
        [MaxLength(200)]
        public string? HeadquartersAddress { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(10)]
        public string? Pincode { get; set; }

        // Branding & Social Media
        public string? LogoPath { get; set; }
        public string? CoverImagePath { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? FacebookUrl { get; set; }

        // Company Culture & Benefits
        [MaxLength(1000)]
        public string? CultureDescription { get; set; }

        [MaxLength(1000)]
        public string? BenefitsOffered { get; set; }

        // Business Status & Verification
        public bool IsVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public DateTime? VerificationDate { get; set; }

        // Subscription & Limits
        public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;
        public int JobPostingLimit { get; set; } = 5; // Based on subscription
        public int ActiveJobPostings { get; set; } = 0;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public List<Recruiter> Recruiters { get; set; } = new();
        public List<Job> Jobs { get; set; } = new();

        // Computed Properties
        public bool CanPostMoreJobs => ActiveJobPostings < JobPostingLimit;
        public int TotalApplicationsReceived => Jobs.Sum(j => j.Applications.Count);
        public double AverageApplicationsPerJob => Jobs.Count > 0 ? (double)TotalApplicationsReceived / Jobs.Count : 0;
    }

    // Enums for Standardization
    public enum Industry
    {
        Technology = 1,
        Healthcare = 2,
        Finance = 3,
        Education = 4,
        Manufacturing = 5,
        Retail = 6,
        Consulting = 7,
        Media = 8,
        NonProfit = 9,
        Government = 10,
        RealEstate = 11,
        Transportation = 12,
        Energy = 13,
        Telecommunications = 14,
        Other = 15
    }

    public enum CompanySize
    {
        Startup = 1,          // 1-10 employees
        Small = 2,            // 11-50 employees
        Medium = 3,           // 51-200 employees
        Large = 4,            // 201-1000 employees
        Enterprise = 5        // 1000+ employees
    }

    public enum SubscriptionPlan
    {
        Free = 1,
        Basic = 2,
        Professional = 3,
        Enterprise = 4
    }
}