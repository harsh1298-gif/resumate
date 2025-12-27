namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class JobRequirement
    {
        public int Id { get; set; } // Add this as primary key
        public int JobId { get; set; }
        public Job? Job { get; set; }

        public int SkillId { get; set; }
        public Skill? Skill { get; set; }
    }
}