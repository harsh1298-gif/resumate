using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Skill
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!; // Remove nullable

        [MaxLength(20)]
        public string? Level { get; set; } // Beginner, Intermediate, Expert

        public int YearsOfExperience { get; set; }

        public string? Category { get; set; } // Technical, Soft, Language, etc.

        // Navigation properties for many-to-many
        public List<ApplicantSkill> ApplicantSkills { get; set; } = new();
        public List<JobRequirement> JobRequirements { get; set; } = new();

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}