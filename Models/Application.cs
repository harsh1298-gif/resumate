using System;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Application
    {
        public int Id { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Additional Application Fields
        [MaxLength(5000)]
        public string? CoverLetter { get; set; }

        [MaxLength(2000)]
        public string? RejectionReason { get; set; }

        [MaxLength(2000)]
        public string? InternalNotes { get; set; }

        public int? ReviewedByRecruiterId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? StatusChangedAt { get; set; }

        // Foreign Keys
        public int ApplicantId { get; set; }
        public int JobId { get; set; }

        // Navigation Properties
        public Applicant? Applicant { get; set; }
        public Job? Job { get; set; }
        public Recruiter? ReviewedBy { get; set; }
        public List<RecruiterNote> Notes { get; set; } = new();
        public List<Interview> Interviews { get; set; } = new();
    }
}
