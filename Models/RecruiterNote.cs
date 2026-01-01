using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class RecruiterNote
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public int RecruiterId { get; set; }

        [Required]
        [MaxLength(5000)]
        public string NoteText { get; set; } = string.Empty;

        public bool IsImportant { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public Application? Application { get; set; }
        public Recruiter? Recruiter { get; set; }
    }
}
