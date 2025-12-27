using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Education
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Institution { get; set; } = null!; // Remove ? and add = null!

        [Required]
        [StringLength(100)]
        public string Degree { get; set; } = null!; // Remove ? and add = null!

        [Required]
        [StringLength(100)]
        public string FieldOfStudy { get; set; } = null!; // Remove ? and add = null!

        public int GraduationYear { get; set; }

        public int ApplicantId { get; set; }
        public Applicant? Applicant { get; set; } // This can stay nullable
    }
}