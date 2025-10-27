using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeProject.Pages
{
    public class RecruiterSignupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RecruiterSignupModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required, Display(Name = "Company Name")]
            public string? CompanyName { get; set; }

            [Required, Url, Display(Name = "Company Website")]
            public string? CompanyWebsite { get; set; }

            [Required, Display(Name = "Industry")]
            public string? Industry { get; set; }

            [Required, Display(Name = "Company Size")]
            public string? CompanySize { get; set; }

            [Display(Name = "Company Logo")]
            public IFormFile? CompanyLogo { get; set; }

            [Required, Display(Name = "Your Full Name")]
            public string? FullName { get; set; }

            [Required, Display(Name = "Job Title")]
            public string? JobTitle { get; set; }

            [Required, EmailAddress, Display(Name = "Work Email")]
            public string? Email { get; set; }

            [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
            public string? Password { get; set; }
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
                OnGet();   // ensures dropdown lists are populated again on form errors
                return Page();
            }

            var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
            var result = await _userManager.CreateAsync(user, Input.Password!);

            if (result.Succeeded)
            {
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

                if (!Enum.TryParse(Input.Industry, out Industry parsedIndustry))
                {
                    ModelState.AddModelError(nameof(Input.Industry), "Invalid Industry selected.");
                    OnGet();
                    return Page();
                }

                if (!Enum.TryParse(Input.CompanySize, out CompanySize parsedCompanySize))
                {
                    ModelState.AddModelError(nameof(Input.CompanySize), "Invalid Company Size selected.");
                    OnGet();
                    return Page();
                }

                var company = new Company
                {
                    Name = Input.CompanyName,
                    Website = Input.CompanyWebsite,
                    Industry = parsedIndustry,
                    CompanySize = parsedCompanySize,
                    LogoPath = logoPath
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                var recruiter = new Recruiter
                {
                    Name = Input.FullName,
                    JobTitle = Input.JobTitle,
                    UserId = user.Id,
                    CompanyId = company.Id
                };
                _context.Recruiters.Add(recruiter);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            OnGet();  // repopulate dropdowns on error
            return Page();
        }
    }
}
