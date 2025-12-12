namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Skill
    {
        public int Id { get; set; }
        public string? Name { get; set; } // Change: Made nullable
        public string? Level { get; set; } // Skill level (Beginner, Intermediate, Advanced, Expert)
        public List<ApplicantSkill> ApplicantSkills { get; set; } = new();
        public List<JobRequirement> JobRequirements { get; set; } = new();
    }
}