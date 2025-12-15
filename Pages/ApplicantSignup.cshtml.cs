using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    public class ApplicantSignupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ApplicantSignupModel> _logger;

        public ApplicantSignupModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<ApplicantSignupModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "Password must be at least 6 characters long", MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Full name is required")]
            [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Date of birth is required")]
            [DataType(DataType.Date)]
            [Range(typeof(DateTime), "1/1/1900", "1/1/2100", ErrorMessage = "Please enter a valid date")]
            public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-18);

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Invalid phone number")]
            [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
            public string PhoneNumber { get; set; } = string.Empty;

            [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
            public string Address { get; set; } = string.Empty;

            [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
            public string City { get; set; } = string.Empty;

            [StringLength(10, ErrorMessage = "Pincode cannot exceed 10 characters")]
            public string Pincode { get; set; } = string.Empty;

            [FileExtensions(Extensions = "jpg,jpeg,png,gif", ErrorMessage = "Please upload a valid image file (JPG, PNG, GIF)")]
            public IFormFile? ProfilePhoto { get; set; }

            [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
            public bool TermsAccepted { get; set; }
        }

        public void OnGet()
        {
            // Initialize with default values
            Input ??= new InputModel();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("=== REGISTRATION PROCESS STARTED ===");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid - showing validation errors");
                foreach (var error in ModelState)
                {
                    if (error.Value?.Errors?.Count > 0)
                    {
                        _logger.LogWarning("Field: {Field}, Error: {Error}",
                            error.Key, error.Value.Errors[0].ErrorMessage);
                    }
                }
                return Page();
            }

            _logger.LogInformation("Model state valid - processing registration for: {Email}", Input.Email);

            IdentityUser? user = null;
            try
            {
                // Check if email already exists
                _logger.LogInformation("Checking for existing user with email: {Email}", Input.Email);
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Email already exists: {Email}", Input.Email);
                    ModelState.AddModelError(nameof(Input.Email), "This email is already registered");
                    return Page();
                }

                // Step 1: Create Identity User
                _logger.LogInformation("Creating Identity user...");
                user = new IdentityUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    EmailConfirmed = true // Set to true for testing
                };

                _logger.LogInformation("Calling UserManager.CreateAsync...");
                var createResult = await _userManager.CreateAsync(user, Input.Password);

                if (!createResult.Succeeded)
                {
                    _logger.LogError("USER CREATION FAILED");
                    foreach (var error in createResult.Errors)
                    {
                        _logger.LogError("Identity error: {Code} - {Description}", error.Code, error.Description);
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }

                _logger.LogInformation("✓ User created successfully with ID: {UserId}", user.Id);

                // Step 2: Create Applicant Profile - Using fully qualified name to avoid namespace conflict
                _logger.LogInformation("Creating Applicant profile...");
                var applicant = new RESUMATE_FINAL_WORKING_MODEL.Models.Applicant
                {
                    UserId = user.Id,
                    Email = Input.Email,
                    FullName = Input.FullName.Trim(),
                    DateOfBirth = Input.DateOfBirth.Date,
                    PhoneNumber = Input.PhoneNumber.Trim(),
                    Address = Input.Address?.Trim(),
                    City = Input.City?.Trim(),
                    Pincode = Input.Pincode?.Trim(),
                    ProfilePhotoPath = null, // Skip photo for testing
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsEmailVerified = true,
                    IsProfileComplete = false
                };

                _logger.LogInformation("Adding applicant to context...");
                _context.Applicants.Add(applicant);

                _logger.LogInformation("Calling SaveChangesAsync...");
                var recordsSaved = await _context.SaveChangesAsync();

                _logger.LogInformation("✓ SaveChangesAsync completed. Records saved: {RecordsSaved}", recordsSaved);
                _logger.LogInformation("✓ Applicant profile created with ID: {ApplicantId}", applicant.Id);

                // Step 3: Add to Applicant role
                _logger.LogInformation("Adding user to Applicant role...");
                var roleResult = await _userManager.AddToRoleAsync(user, "Applicant");
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("✓ User added to Applicant role successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to add user to Applicant role, but continuing...");
                }

                // Step 4: Sign in user
                _logger.LogInformation("Signing in user...");
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("✓ User signed in successfully");

                TempData["SuccessMessage"] = $"Registration successful! Welcome {Input.FullName}";
                _logger.LogInformation("=== REGISTRATION PROCESS COMPLETED SUCCESSFULLY ===");

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ CRITICAL ERROR during registration process");

                // Cleanup if user was created but applicant profile failed
                if (user != null)
                {
                    try
                    {
                        _logger.LogInformation("Cleaning up partially created user...");
                        await _userManager.DeleteAsync(user);
                        _logger.LogInformation("User cleanup completed");
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to cleanup user after registration error");
                    }
                }

                ModelState.AddModelError(string.Empty,
                    "A system error occurred during registration. Please try again.");
                return Page();
            }
        }
    }
}
