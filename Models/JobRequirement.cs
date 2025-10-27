namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class JobRequirement
    {
        public int JobId { get; set; }
        public Job? Job { get; set; } // Change: Made nullable

        public int SkillId { get; set; }
        public Skill? Skill { get; set; } // Change: Made nullable
    }
}