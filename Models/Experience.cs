using System;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Experience
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Company { get; set; } = null!; // Remove ? and add = null!

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = null!; // Remove ? and add = null!

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public int ApplicantId { get; set; }
        public Applicant? Applicant { get; set; } // This can stay nullable
    }
}