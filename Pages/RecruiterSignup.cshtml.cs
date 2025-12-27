using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    public class RecruiterSignupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RecruiterSignupModel> _logger;

        public RecruiterSignupModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<RecruiterSignupModel> logger)
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
            [Required(ErrorMessage = "Company name is required")]
            [StringLength(200)]
            [Display(Name = "Company Name")]
            public string CompanyName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Company website is required")]
            [Url]
            [Display(Name = "Company Website")]
            public string CompanyWebsite { get; set; } = string.Empty;

            [Required(ErrorMessage = "Company description is required")]
            [StringLength(1000, MinimumLength = 50)]
            [Display(Name = "Company Description")]
            public string CompanyDescription { get; set; } = string.Empty;

            [Required(ErrorMessage = "Industry is required")]
            [Display(Name = "Industry")]
            public string Industry { get; set; } = string.Empty;

            [Required(ErrorMessage = "Company size is required")]
            [Display(Name = "Company Size")]
            public string CompanySize { get; set; } = string.Empty;

            [Display(Name = "Company Logo")]
            public IFormFile? CompanyLogo { get; set; }

            [Required(ErrorMessage = "Full name is required")]
            [StringLength(100)]
            [Display(Name = "Your Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Job title is required")]
            [StringLength(100)]
            [Display(Name = "Job Title")]
            public string JobTitle { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress]
            [Display(Name = "Work Email")]
            public string Email { get; set; } = string.Empty;

            [Phone]
            [Display(Name = "Phone Number")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirm password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public List<SelectListItem> IndustryList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CompanySizeList { get; set; } = new List<SelectListItem>();

        public void OnGet()
        {
            IndustryList = Enum.GetValues(typeof(Industry))
                .Cast<Industry>()
                .Select(i => new SelectListItem { Text = i.ToString(), Value = i.ToString() })
                .ToList();

            CompanySizeList = Enum.GetValues(typeof(CompanySize))
                .Cast<CompanySize>()
                .Select(cs => new SelectListItem { Text = cs.ToString(), Value = cs.ToString() })
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                OnGet();
                return Page();
            }

            try
            {
                // Check if user exists
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Input.Email", "User already exists.");
                    OnGet();
                    return Page();
                }

                // Create user
                var user = new IdentityUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    EmailConfirmed = true,
                    PhoneNumber = Input.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // Handle logo upload
                    string? logoPath = null;
                    if (Input.CompanyLogo != null)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logos");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.CompanyLogo.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            await Input.CompanyLogo.CopyToAsync(fs);
                        }
                        logoPath = $"/uploads/logos/{uniqueFileName}";
                    }

                    // Parse enums
                    if (!Enum.TryParse(Input.Industry, out Industry parsedIndustry))
                    {
                        ModelState.AddModelError(nameof(Input.Industry), "Invalid Industry.");
                        await _userManager.DeleteAsync(user);
                        OnGet();
                        return Page();
                    }

                    if (!Enum.TryParse(Input.CompanySize, out CompanySize parsedCompanySize))
                    {
                        ModelState.AddModelError(nameof(Input.CompanySize), "Invalid Company Size.");
                        await _userManager.DeleteAsync(user);
                        OnGet();
                        return Page();
                    }

                    // Create Company
                    var company = new Company
                    {
                        Name = Input.CompanyName,
                        Website = Input.CompanyWebsite,
                        Description = Input.CompanyDescription,
                        Industry = parsedIndustry,
                        CompanySize = parsedCompanySize,
                        LogoPath = logoPath,
                        ContactEmail = Input.Email
                    };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    // Create Recruiter
                    var recruiter = new Recruiter
                    {
                        Name = Input.FullName,
                        Email = Input.Email,
                        JobTitle = Input.JobTitle,
                        PhoneNumber = Input.PhoneNumber ?? string.Empty,
                        UserId = user.Id,
                        CompanyId = company.Id
                    };
                    _context.Recruiters.Add(recruiter);
                    await _context.SaveChangesAsync();

                    // Add to Recruiter role
                    await _userManager.AddToRoleAsync(user, "Recruiter");

                    // Sign in
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToPage("/RecruiterDashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recruiter signup");
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
            }

            OnGet();
            return Page();
        }
    }
}