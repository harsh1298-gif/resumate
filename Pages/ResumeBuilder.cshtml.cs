using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

// Alias to resolve namespace conflict
using ApplicantModel = RESUMATE_FINAL_WORKING_MODEL.Models.Applicant;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    [Authorize(Roles = "Applicant")]
    public class ResumeBuilderModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ResumeBuilderModel(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [BindProperty]
        public ResumeUploadInput Input { get; set; } = new ResumeUploadInput();

        public ApplicantModel? CurrentApplicant { get; set; }
        public bool HasExistingResume { get; set; }
        public string? CurrentResumeFileName { get; set; }
        public string? CurrentResumeFilePath { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            CurrentApplicant = await _context.Applicants
                .Include(a => a.Experiences)
                .Include(a => a.Educations)
                .Include(a => a.ApplicantSkills)
                    .ThenInclude(ask => ask.Skill)
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (CurrentApplicant == null)
            {
                TempData["Error"] = "Please complete your profile first.";
                return RedirectToPage("/EditProfile");
            }

            // Compute HasResume based on whether ResumeFilePath has a value
            HasExistingResume = !string.IsNullOrEmpty(CurrentApplicant.ResumeFilePath);
            CurrentResumeFileName = CurrentApplicant.ResumeFileName;
            CurrentResumeFilePath = CurrentApplicant.ResumeFilePath;

            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (Input.ResumeFile == null)
            {
                ModelState.AddModelError("Input.ResumeFile", "Please select a PDF file to upload.");
                await OnGetAsync();
                return Page();
            }

            // Validate file type - PDF ONLY
            var fileExtension = Path.GetExtension(Input.ResumeFile.FileName).ToLowerInvariant();

            if (fileExtension != ".pdf")
            {
                ModelState.AddModelError("Input.ResumeFile", "Only PDF files are allowed. Please convert your resume to PDF format.");
                await OnGetAsync();
                return Page();
            }

            // Validate file size (max 5MB)
            if (Input.ResumeFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("Input.ResumeFile", "File size must not exceed 5MB.");
                await OnGetAsync();
                return Page();
            }

            // Validate minimum file size (at least 10KB to ensure it's not empty)
            if (Input.ResumeFile.Length < 10 * 1024)
            {
                ModelState.AddModelError("Input.ResumeFile", "File is too small. Please upload a valid PDF resume.");
                await OnGetAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (applicant == null)
            {
                TempData["Error"] = "Applicant profile not found.";
                return RedirectToPage("/EditProfile");
            }

            try
            {
                // Delete old resume if exists
                if (!string.IsNullOrEmpty(applicant.ResumeFilePath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, applicant.ResumeFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new resume
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = string.Format("{0}_{1}", Guid.NewGuid(), Path.GetFileName(Input.ResumeFile.FileName));
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ResumeFile.CopyToAsync(fileStream);
                }

                // Update applicant record
                applicant.ResumeFileName = Input.ResumeFile.FileName;
                applicant.ResumeFilePath = string.Format("/uploads/resumes/{0}", uniqueFileName);
                applicant.ResumeUploadDate = DateTime.UtcNow;
                applicant.UpdateTimestamps();

                await _context.SaveChangesAsync();

                TempData["Success"] = "Resume uploaded successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, string.Format("Error uploading resume: {0}", ex.Message));
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var applicant = await _context.Applicants
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (applicant == null || string.IsNullOrEmpty(applicant.ResumeFilePath))
            {
                TempData["Error"] = "No resume found to delete.";
                return RedirectToPage();
            }

            try
            {
                // Delete file from server
                var filePath = Path.Combine(_environment.WebRootPath, applicant.ResumeFilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Update database
                applicant.ResumeFileName = null;
                applicant.ResumeFilePath = null;
                applicant.ResumeUploadDate = null;
                applicant.UpdateTimestamps();

                await _context.SaveChangesAsync();

                TempData["Success"] = "Resume deleted successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Error"] = string.Format("Error deleting resume: {0}", ex.Message);
                return RedirectToPage();
            }
        }

        public string GetFileSize(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "Unknown";

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    var sizeInKB = fileInfo.Length / 1024.0;
                    var sizeInMB = sizeInKB / 1024.0;

                    if (sizeInMB >= 1)
                        return string.Format("{0:F2} MB", sizeInMB);
                    else
                        return string.Format("{0:F2} KB", sizeInKB);
                }
            }
            catch
            {
                // Silently handle any file access errors
            }

            return "Unknown";
        }

        public string GetFileIcon(string? fileName)
        {
            // Always return PDF icon since we only accept PDFs
            return "fas fa-file-pdf text-red-600";
        }
    }

    public class ResumeUploadInput
    {
        [Required(ErrorMessage = "Please select a PDF file to upload")]
        [Display(Name = "Resume File (PDF Only)")]
        public IFormFile? ResumeFile { get; set; }
    }
}