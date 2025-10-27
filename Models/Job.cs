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
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Location { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Salary { get; set; }

        public ExperienceLevel ExperienceLevel { get; set; }
        public JobType Type { get; set; }
        public JobCategory Category { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }

        // UNCOMMENTED - Add Recruiter back
        public int RecruiterId { get; set; }
        public Recruiter Recruiter { get; set; }

        // UNCOMMENTED - Add collections back
        public List<JobRequirement> RequiredSkills { get; set; } = new();
        public List<Application> Applications { get; set; } = new();

        public string? AttachmentPath { get; set; }
        public string? ApplicationLink { get; set; }

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