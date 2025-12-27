using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Applicant
    {
        public int Id { get; set; }

        // Basic Information - WITH DATABASE COLUMN MAPPING
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Phone]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? Pincode { get; set; }

        // Profile Enhancement
        public string? ProfilePhotoPath { get; set; }

        [MaxLength(1000)]
        public string? ProfessionalSummary { get; set; }

        [MaxLength(500)]
        public string? Objective { get; set; }

        // Resume Management
        public string? ResumeFilePath { get; set; }

        public string? ResumeFileName { get; set; }

        public DateTime? ResumeUploadDate { get; set; }

        // Profile Status
        public bool IsProfileComplete { get; set; }

        public bool IsEmailVerified { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Identity Integration
        public string? UserId { get; set; }

        // Navigation Properties - CORRECTED
        public virtual List<ApplicantSkill> ApplicantSkills { get; set; } = new List<ApplicantSkill>();
        public virtual List<Experience> Experiences { get; set; } = new List<Experience>();
        public virtual List<Education> Educations { get; set; } = new List<Education>();
        public virtual List<Application> Applications { get; set; } = new List<Application>();

        // Computed Properties
        [NotMapped]
        public bool HasResume => !string.IsNullOrEmpty(ResumeFilePath);

        [NotMapped]
        public int Age => CalculateAge();

        [NotMapped]
        public int TotalExperienceYears => CalculateTotalExperience();

        [NotMapped]
        public string FullAddress => GetFullAddress();

        [NotMapped]
        public bool IsProfilePublic => IsActive && IsProfileComplete && IsEmailVerified;

        // Method to calculate age
        private int CalculateAge()
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        // Method to calculate total experience
        private int CalculateTotalExperience()
        {
            if (Experiences == null || !Experiences.Any())
                return 0;

            var totalMonths = Experiences.Sum(exp =>
            {
                var endDate = exp.EndDate ?? DateTime.Now;
                return ((endDate.Year - exp.StartDate.Year) * 12) + (endDate.Month - exp.StartDate.Month);
            });

            return totalMonths / 12;
        }

        // Method to get full address
        private string GetFullAddress()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Address)) parts.Add(Address);
            if (!string.IsNullOrEmpty(City)) parts.Add(City);
            if (!string.IsNullOrEmpty(Pincode)) parts.Add(Pincode);
            return string.Join(", ", parts);
        }

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

            // Remove any non-digit characters
            var digits = new string(PhoneNumber.Where(char.IsDigit).ToArray());

            if (digits.Length == 10)
                return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}";

            return PhoneNumber;
        }

        public string GetInitials()
        {
            if (string.IsNullOrEmpty(FullName))
                return "??";

            var names = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length >= 2)
                return $"{names[0][0]}{names[1][0]}".ToUpper();
            else if (names[0].Length >= 2)
                return names[0].Substring(0, 2).ToUpper();
            else
                return names[0].ToUpper();
        }

        // Profile completion calculation
        [NotMapped]
        public int ProfileCompletionPercentage => CalculateProfileCompletion();

        private int CalculateProfileCompletion()
        {
            var fields = new (bool IsComplete, int Weight)[]
            {
                (!string.IsNullOrEmpty(FullName), 15),
                (!string.IsNullOrEmpty(Email), 10),
                (!string.IsNullOrEmpty(PhoneNumber), 10),
                (DateOfBirth != default(DateTime), 10),
                (!string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(City), 10),
                (!string.IsNullOrEmpty(ProfessionalSummary), 15),
                (!string.IsNullOrEmpty(Objective), 10),
                (ApplicantSkills?.Any() == true, 10),
                (Experiences?.Any() == true, 5),
                (Educations?.Any() == true, 5)
            };

            return fields.Sum(field => field.IsComplete ? field.Weight : 0);
        }
    }
}