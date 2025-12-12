using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Match your existing model classes
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<JobRequirement> JobRequirements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ApplicantSkill composite primary key
            modelBuilder.Entity<ApplicantSkill>()
                .HasKey(a => new { a.ApplicantId, a.SkillId });

            modelBuilder.Entity<ApplicantSkill>()
                .HasOne(a => a.Applicant)
                .WithMany(a => a.ApplicantSkills)
                .HasForeignKey(a => a.ApplicantId);

            modelBuilder.Entity<ApplicantSkill>()
                .HasOne(a => a.Skill)
                .WithMany(s => s.ApplicantSkills)
                .HasForeignKey(a => a.SkillId);

            // Map to actual table names
            modelBuilder.Entity<Applicant>().ToTable("Applicants");
            modelBuilder.Entity<Job>().ToTable("Jobs");
            modelBuilder.Entity<Company>().ToTable("Companies");
            modelBuilder.Entity<Application>().ToTable("Applications");

            // Configure enums to store as strings
            modelBuilder.Entity<Job>()
                .Property(j => j.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Job>()
                .Property(j => j.Category)
                .HasConversion<string>();

            modelBuilder.Entity<Job>()
                .Property(j => j.ExperienceLevel)
                .HasConversion<string>();
        }
    }
}