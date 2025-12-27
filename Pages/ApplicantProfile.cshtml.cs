using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System.ComponentModel.DataAnnotations;
using ApplicantModel = RESUMATE_FINAL_WORKING_MODEL.Models.Applicant;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    [Authorize]
    public class ApplicantProfileModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ApplicantProfileModel> _logger;
        private readonly IWebHostEnvironment _environment;

        [BindProperty]
        public ApplicantModel? Applicant { get; set; }

        [BindProperty]
        public IFormFile? ProfilePhoto { get; set; }

        [BindProperty]
        public IFormFile? ResumeFile { get; set; }

        public ApplicantProfileModel(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<ApplicantProfileModel> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _environment = environment;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to profile page");
                    return RedirectToPage("/Login");
                }

                Applicant = await _context.Applicants
                    .Include(a => a.ApplicantSkills)
                        .ThenInclude(appSkill => appSkill.Skill)
                    .Include(a => a.Experiences)
                    .Include(a => a.Educations)
                    .FirstOrDefaultAsync(a => a.UserId == user.Id);

                if (Applicant == null)
                {
                    _logger.LogWarning("Applicant profile not found for user {UserId}", user.Id);
                    TempData["ErrorMessage"] = "Profile not found. Please contact support.";
                    return RedirectToPage("/Dashboard");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading applicant profile");
                TempData["ErrorMessage"] = "Error loading profile. Please try again.";
                return RedirectToPage("/Dashboard");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Profile update started");

                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Unauthorized profile update attempt");
                    return RedirectToPage("/Login");
                }

                _logger.LogInformation("User authenticated: {UserId}", user.Id);

                // Get existing applicant from database
                var existingApplicant = await _context.Applicants
                    .FirstOrDefaultAsync(a => a.UserId == user.Id);

                if (existingApplicant == null)
                {
                    _logger.LogWarning("Profile not found for user {UserId} during update", user.Id);
                    TempData["ErrorMessage"] = "Profile not found.";
                    return RedirectToPage("/Dashboard");
                }

                _logger.LogInformation("Existing applicant found: {ApplicantId}", existingApplicant.Id);

                // Validate input data
                if (Applicant == null)
                {
                    _logger.LogWarning("Applicant model is null in POST");
                    TempData["ErrorMessage"] = "Invalid profile data.";
                    return Page();
                }

                _logger.LogInformation("Applicant data received - Name: {Name}, Email: {Email}", 
                    Applicant.FullName, Applicant.Email);

                // Security: Prevent changing UserId
                if (!string.IsNullOrEmpty(Applicant.UserId) && Applicant.UserId != user.Id)
                {
                    _logger.LogWarning("Attempt to change UserId detected for user {UserId}", user.Id);
                    TempData["ErrorMessage"] = "Security violation detected.";
                    return RedirectToPage("/Dashboard");
                }

                // Update only allowed fields
                existingApplicant.FullName = SanitizeInput(Applicant.FullName) ?? string.Empty;
                existingApplicant.Email = SanitizeInput(Applicant.Email) ?? string.Empty;
                existingApplicant.PhoneNumber = SanitizeInput(Applicant.PhoneNumber) ?? string.Empty;
                existingApplicant.DateOfBirth = Applicant.DateOfBirth;
                existingApplicant.Address = SanitizeInput(Applicant.Address);
                existingApplicant.City = SanitizeInput(Applicant.City);
                existingApplicant.Pincode = SanitizeInput(Applicant.Pincode);
                existingApplicant.ProfessionalSummary = SanitizeInput(Applicant.ProfessionalSummary);
                existingApplicant.Objective = SanitizeInput(Applicant.Objective);
                existingApplicant.UpdatedAt = DateTime.UtcNow;

                // Mark entity as modified to ensure EF tracks changes
                _context.Entry(existingApplicant).State = EntityState.Modified;

                // Handle profile photo upload
                if (ProfilePhoto != null && ProfilePhoto.Length > 0)
                {
                    var photoPath = await SaveProfilePhoto(ProfilePhoto, user.Id);
                    if (photoPath != null)
                    {
                        existingApplicant.ProfilePhotoPath = photoPath;
                    }
                }

                // Handle resume upload
                if (ResumeFile != null && ResumeFile.Length > 0)
                {
                    var resumePath = await SaveResume(ResumeFile, user.Id);
                    if (resumePath != null)
                    {
                        existingApplicant.ResumeFilePath = resumePath;
                        existingApplicant.ResumeFileName = ResumeFile.FileName;
                        existingApplicant.ResumeUploadDate = DateTime.UtcNow;
                    }
                }

                // Calculate profile completion
                existingApplicant.IsProfileComplete = CalculateProfileCompletion(existingApplicant) >= 80;

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated successfully for user {UserId}", user.Id);
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToPage("/Dashboard");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error updating applicant profile");
                TempData["ErrorMessage"] = "Database error. Please try again.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating applicant profile");
                TempData["ErrorMessage"] = "Error updating profile. Please try again.";
                return Page();
            }
        }

        private string? SanitizeInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Remove potentially dangerous characters and trim
            return input.Trim();
        }

        private async Task<string?> SaveProfilePhoto(IFormFile file, string userId)
        {
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["WarningMessage"] = "Invalid photo format. Only JPG, PNG, and GIF are allowed.";
                    return null;
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    TempData["WarningMessage"] = "Photo size must be less than 5MB.";
                    return null;
                }

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/uploads/profiles/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile photo for user {UserId}", userId);
                TempData["WarningMessage"] = "Error uploading photo.";
                return null;
            }
        }

        private async Task<string?> SaveResume(IFormFile file, string userId)
        {
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["WarningMessage"] = "Invalid resume format. Only PDF, DOC, and DOCX are allowed.";
                    return null;
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    TempData["WarningMessage"] = "Resume size must be less than 10MB.";
                    return null;
                }

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/uploads/resumes/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving resume for user {UserId}", userId);
                TempData["WarningMessage"] = "Error uploading resume.";
                return null;
            }
        }

        private int CalculateProfileCompletion(ApplicantModel applicant)
        {
            var fields = new (bool IsComplete, int Weight)[]
            {
                (!string.IsNullOrEmpty(applicant.FullName), 15),
                (!string.IsNullOrEmpty(applicant.Email), 10),
                (!string.IsNullOrEmpty(applicant.PhoneNumber), 10),
                (applicant.DateOfBirth != default(DateTime), 10),
                (!string.IsNullOrEmpty(applicant.Address) && !string.IsNullOrEmpty(applicant.City), 10),
                (!string.IsNullOrEmpty(applicant.ProfessionalSummary), 15),
                (!string.IsNullOrEmpty(applicant.Objective), 10),
                (!string.IsNullOrEmpty(applicant.ResumeFilePath), 10),
                (!string.IsNullOrEmpty(applicant.ProfilePhotoPath), 5),
                (applicant.ApplicantSkills?.Any() == true, 5)
            };

            return fields.Sum(field => field.IsComplete ? field.Weight : 0);
        }
    }
}