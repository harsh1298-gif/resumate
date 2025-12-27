using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets for all your models
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<ApplicantSkill> ApplicantSkills { get; set; }
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<JobRequirement> JobRequirements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Applicant
            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.FullName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Email).IsRequired().HasMaxLength(255);
                entity.Property(a => a.PhoneNumber).IsRequired().HasMaxLength(15);
                entity.Property(a => a.DateOfBirth).IsRequired();
                entity.Property(a => a.City).HasMaxLength(50);
                entity.Property(a => a.ProfilePhotoPath);
                entity.Property(a => a.ProfessionalSummary).HasMaxLength(1000);
                entity.Property(a => a.Objective).HasMaxLength(500);
                entity.Property(a => a.ResumeFilePath);
                entity.Property(a => a.ResumeFileName);
                entity.Property(a => a.ResumeUploadDate);
                entity.Property(a => a.CreatedAt);
                entity.Property(a => a.UpdatedAt);

                // Keep only the direct ApplicantSkills relationship
                entity.HasMany(a => a.ApplicantSkills)
                      .WithOne(appSkill => appSkill.Applicant)
                      .HasForeignKey(appSkill => appSkill.ApplicantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.Experiences)
                      .WithOne(e => e.Applicant)
                      .HasForeignKey(e => e.ApplicantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.Educations)
                      .WithOne(e => e.Applicant)
                      .HasForeignKey(e => e.ApplicantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.Applications)
                      .WithOne(app => app.Applicant)
                      .HasForeignKey(app => app.ApplicantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Skill
            modelBuilder.Entity<Skill>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Level).HasMaxLength(20);

                // Keep only the ApplicantSkills relationship
                entity.HasMany(s => s.ApplicantSkills)
                      .WithOne(appSkill => appSkill.Skill)
                      .HasForeignKey(appSkill => appSkill.SkillId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ApplicantSkill (junction table)
            modelBuilder.Entity<ApplicantSkill>(entity =>
            {
                entity.HasKey(appSkill => new { appSkill.ApplicantId, appSkill.SkillId });

                entity.HasOne(appSkill => appSkill.Applicant)
                      .WithMany(a => a.ApplicantSkills)
                      .HasForeignKey(appSkill => appSkill.ApplicantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(appSkill => appSkill.Skill)
                      .WithMany(s => s.ApplicantSkills)
                      .HasForeignKey(appSkill => appSkill.SkillId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Experience
            modelBuilder.Entity<Experience>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Company).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
            });

            // Configure Education
            modelBuilder.Entity<Education>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Institution).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Degree).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FieldOfStudy).IsRequired().HasMaxLength(100);
            });

            // Configure Company
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).IsRequired().HasMaxLength(2000);
                entity.Property(c => c.ContactEmail).IsRequired();

                entity.HasMany(c => c.Recruiters)
                      .WithOne(r => r.Company)
                      .HasForeignKey(r => r.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Jobs)
                      .WithOne(j => j.Company)
                      .HasForeignKey(j => j.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Job
            modelBuilder.Entity<Job>(entity =>
            {
                entity.HasKey(j => j.Id);
                entity.Property(j => j.Title).IsRequired().HasMaxLength(100);
                entity.Property(j => j.Description).IsRequired();
                entity.Property(j => j.Location).IsRequired();
                entity.Property(j => j.Salary).HasColumnType("decimal(18,2)");

                entity.HasMany(j => j.Applications)
                      .WithOne(app => app.Job)
                      .HasForeignKey(app => app.JobId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Recruiter
            modelBuilder.Entity<Recruiter>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Email).IsRequired();
                entity.Property(r => r.PhoneNumber).IsRequired().HasMaxLength(15);

                entity.HasMany(r => r.Jobs)
                      .WithOne(j => j.Recruiter)
                      .HasForeignKey(j => j.RecruiterId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Application
            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Status).IsRequired().HasDefaultValue("Pending");
            });
        }
    }
}