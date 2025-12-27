namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class ApplicantSkill
    {
        public int ApplicantId { get; set; }
        public Applicant? Applicant { get; set; } // Change

        public int SkillId { get; set; }
        public Skill? Skill { get; set; } // Change
    }
}
