using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Salary { get; set; }

        public ExperienceLevel ExperienceLevel { get; set; }
        public JobType Type { get; set; }
        public JobCategory Category { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // UNCOMMENTED - Add Recruiter back
        public int RecruiterId { get; set; }
        public Recruiter Recruiter { get; set; } = null!;

        // UNCOMMENTED - Add collections back
        public List<JobRequirement> RequiredSkills { get; set; } = new();
        public List<Application> Applications { get; set; } = new();

        public string? AttachmentPath { get; set; }
        public string? ApplicationLink { get; set; }

        // Additional Job Details
        [MaxLength(2000)]
        public string? Benefits { get; set; }

        [Range(1, 999)]
        public int NumberOfOpenings { get; set; } = 1;

        public bool RemoteWorkOption { get; set; } = false;

        [MaxLength(50)]
        public string? RemoteWorkType { get; set; } // Remote, Hybrid, OnSite

        public DateTime? ApplicationDeadline { get; set; }

        public int ViewCount { get; set; } = 0;

        [MaxLength(5000)]
        public string? KeyResponsibilities { get; set; } // Store as JSON array

        [MaxLength(2000)]
        public string? PreferredQualifications { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ClosingDate { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum JobType { FullTime, PartTime, Internship, Contract, Temporary }
    public enum JobCategory { Engineering, Marketing, Sales, Design, HR, Other }
    public enum ExperienceLevel { Entry, Mid, Senior, Director }
}