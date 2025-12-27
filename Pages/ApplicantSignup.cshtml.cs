using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    public class ApplicantSignupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ApplicantSignupModel> _logger;
        private readonly AppDbContext _context;

        public ApplicantSignupModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IWebHostEnvironment environment,
            ILogger<ApplicantSignupModel> logger,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _logger = logger;
            _context = context;
            Input = new InputModel();
            ReturnUrl = string.Empty;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirm password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Full name is required")]
            [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Date of birth is required")]
            [Display(Name = "Date of Birth")]
            [DataType(DataType.Date)]
            public DateTime DateOfBirth { get; set; }

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Invalid phone number")]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; } = string.Empty;

            [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
            [Display(Name = "Address")]
            public string? Address { get; set; }

            [StringLength(50, ErrorMessage = "City name cannot exceed 50 characters")]
            [Display(Name = "City")]
            public string? City { get; set; }

            [StringLength(10, ErrorMessage = "Pincode cannot exceed 10 characters")]
            [Display(Name = "Pincode")]
            public string? Pincode { get; set; }

            [Display(Name = "Profile Photo")]
            [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".gif" })]
            [MaxFileSize(5 * 1024 * 1024)]
            public IFormFile? ProfilePhoto { get; set; }

            [Required(ErrorMessage = "You must accept the terms and conditions")]
            [Display(Name = "I accept the terms and conditions")]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
            public bool TermsAccepted { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            Input = new InputModel
            {
                DateOfBirth = DateTime.Now.AddYears(-18)
            };
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Validate age (at least 18 years old)
                var age = DateTime.Now.Year - Input.DateOfBirth.Year;
                if (Input.DateOfBirth > DateTime.Now.AddYears(-age)) age--;

                if (age < 18)
                {
                    ModelState.AddModelError("Input.DateOfBirth", "You must be at least 18 years old to register.");
                    return Page();
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Input.Email", "A user with this email already exists.");
                    return Page();
                }

                // Create user
                var user = new IdentityUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    PhoneNumber = Input.PhoneNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Handle profile photo upload
                    string? profilePhotoPath = null;
                    if (Input.ProfilePhoto != null && Input.ProfilePhoto.Length > 0)
                    {
                        profilePhotoPath = await SaveProfilePhotoAsync(user.Id, Input.ProfilePhoto);
                    }

                    // Save to Applicants table
                    var applicant = new RESUMATE_FINAL_WORKING_MODEL.Models.Applicant
                    {
                        UserId = user.Id,
                        Email = Input.Email,
                        FullName = Input.FullName,
                        DateOfBirth = Input.DateOfBirth,
                        PhoneNumber = Input.PhoneNumber,
                        Address = Input.Address ?? string.Empty,
                        City = Input.City ?? string.Empty,
                        Pincode = Input.Pincode ?? string.Empty,
                        ProfilePhotoPath = profilePhotoPath,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsProfileComplete = false,
                        IsEmailVerified = false
                    };

                    // Save applicant to database
                    _context.Applicants.Add(applicant);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Applicant profile created for user: {Email}", Input.Email);

                    // Add user to Applicant role
                    await _userManager.AddToRoleAsync(user, "Applicant");

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    TempData["SuccessMessage"] = $"Welcome {Input.FullName}! Your account has been created successfully.";
                    _logger.LogInformation("User logged in after registration.");

                    return RedirectToPage("/Dashboard");
                }

                // Add errors to ModelState
                foreach (var error in result.Errors)
                {
                    if (error.Code.Contains("Password"))
                    {
                        ModelState.AddModelError("Input.Password", error.Description);
                    }
                    else if (error.Code.Contains("Email") || error.Code.Contains("User"))
                    {
                        ModelState.AddModelError("Input.Email", error.Description);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user account.");
                ModelState.AddModelError(string.Empty, "An error occurred while creating your account. Please try again.");
            }

            return Page();
        }

        private async Task<string?> SaveProfilePhotoAsync(string userId, IFormFile photo)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                var uniqueFileName = $"{userId}_{DateTime.UtcNow.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                return $"/uploads/profiles/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile photo for user {UserId}", userId);
                return null;
            }
        }
    }

    // Custom validation attributes
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult($"Only {string.Join(", ", _extensions)} files are allowed.");
                }
            }

            return ValidationResult.Success;
        }
    }

    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;

        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult($"Maximum file size is {_maxFileSize / 1024 / 1024}MB.");
                }
            }

            return ValidationResult.Success;
        }
    }
}