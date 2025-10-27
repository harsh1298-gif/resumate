using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Education
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string? Institution { get; set; } // Change

        [Required]
        [StringLength(100)]
        public string? Degree { get; set; } // Change

        [Required]
        [StringLength(100)]
        public string? FieldOfStudy { get; set; } // Change

        public int GraduationYear { get; set; }

        public int ApplicantId { get; set; }
        public Applicant? Applicant { get; set; } // Change
    }
}