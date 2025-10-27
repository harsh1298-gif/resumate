using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Applicant
    {
        public int Id { get; set; }

        // Basic Information - WITH DATABASE COLUMN MAPPING
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [Column("DataEdit")]  // Maps to database column
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Phone]
        [MaxLength(15)]
        [Column("FrameView")]  // Maps to database column
        public string PhoneNumber { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(50)]
        [Column("Cys")]  // Maps to database column
        public string? City { get; set; }

        [MaxLength(10)]
        public string? Pincode { get; set; }

        // Profile Enhancement
        [Column("DefaultDataPath")]  // Maps to database column
        public string? ProfilePhotoPath { get; set; }

        [MaxLength(1000)]
        [Column("ReferenceSummary")]  // Maps to database column
        public string? ProfessionalSummary { get; set; }

        [MaxLength(500)]
        [Column("Object")]  // Maps to database column
        public string? Objective { get; set; }

        // Resume Management
        [Column("ReturnFilePath")]
        public string? ResumeFilePath { get; set; }

        [Column("ReturnFileName")]
        public string? ResumeFileName { get; set; }

        [Column("Traceback")]
        public DateTime? ResumeUploadDate { get; set; }

        // Profile Status
        public bool IsProfileComplete { get; set; }

        [Column("IsEmail/InfoId")]
        public bool IsEmailVerified { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit Fields
        [Column("Subset")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("Console")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Identity Integration
        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }

        // Navigation Properties - MATCHING YOUR EXISTING MODELS
        public List<Skill> Skills { get; set; } = new();
        public List<Experience> Experiences { get; set; } = new();
        public List<Education> Educations { get; set; } = new();
        public List<Application> Applications { get; set; } = new();

        // Computed Properties
        [NotMapped]
        public bool HasResume => !string.IsNullOrEmpty(ResumeFilePath);

        [NotMapped]
        public int Age => DateTime.Now.Year - DateOfBirth.Year;

        [NotMapped]
        public int TotalExperienceYears
        {
            get
            {
                if (Experiences == null || !Experiences.Any())
                    return 0;

                return Experiences.Sum(e =>
                    (e.EndDate?.Year ?? DateTime.Now.Year) - e.StartDate.Year);
            }
        }

        [NotMapped]
        public string FullAddress
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(Address)) parts.Add(Address);
                if (!string.IsNullOrEmpty(City)) parts.Add(City);
                if (!string.IsNullOrEmpty(Pincode)) parts.Add(Pincode);
                return string.Join(", ", parts);
            }
        }

        [NotMapped]
        public bool IsProfilePublic => IsActive && IsProfileComplete && IsEmailVerified;

        // Method to update timestamp
        public void UpdateTimestamps()
        {
            UpdatedAt = DateTime.UtcNow;
            if (CreatedAt == DateTime.MinValue)
                CreatedAt = DateTime.UtcNow;
        }

        // Validation method
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(FullName) &&
                   !string.IsNullOrEmpty(Email) &&
                   !string.IsNullOrEmpty(PhoneNumber) &&
                   DateOfBirth < DateTime.Now.AddYears(-16); // At least 16 years old
        }

        // Formatting methods
        public string GetFormattedPhone()
        {
            if (string.IsNullOrEmpty(PhoneNumber))
                return string.Empty;

            if (PhoneNumber.Length == 10)
                return $"({PhoneNumber.Substring(0, 3)}) {PhoneNumber.Substring(3, 3)}-{PhoneNumber.Substring(6)}";

            return PhoneNumber;
        }

        public string GetInitials()
        {
            if (string.IsNullOrEmpty(FullName))
                return "??";

            var names = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return names.Length >= 2
                ? $"{names[0][0]}{names[1][0]}".ToUpper()
                : names[0].Substring(0, Math.Min(2, names[0].Length)).ToUpper();
        }
    }
}