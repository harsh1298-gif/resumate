using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Recruiter
    {
        public int Id { get; set; }

        // Basic Information
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [Required]
        [Phone]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = null!;

        [MaxLength(200)]
        public string? Address { get; set; }

        // Profile Enhancement
        public string? ProfilePhotoPath { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        // Hiring Permissions & Role Management
        public HiringRole Role { get; set; } = HiringRole.Recruiter;
        public bool CanPostJobs { get; set; } = true;
        public bool CanViewAllApplications { get; set; } = true;
        public bool CanMakeHiringDecisions { get; set; } = false;
        public bool CanManageTeam { get; set; } = false;

        // Account Status
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; }
        public bool IsProfileComplete { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }

        // Identity Integration
        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }

        // Company Relationship
        [Required]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // Job Management
        public List<Job> Jobs { get; set; } = new();

        // Team Management (Optional - for future enhancement)
        public int? ManagerId { get; set; }
        public Recruiter? Manager { get; set; }
        public List<Recruiter> TeamMembers { get; set; } = new();

        // Computed Properties
        public int ActiveJobsCount => Jobs.Count(j => j.IsActive);
        public int TotalApplicationsReceived => Jobs.Sum(j => j.Applications.Count);
    }

    // Enum for Hiring Roles
    public enum HiringRole
    {
        Recruiter = 1,
        SeniorRecruiter = 2,
        HiringManager = 3,
        HRDirector = 4,
        Administrator = 5
    }
}