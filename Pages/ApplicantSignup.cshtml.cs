using Microsoft.AspNetCore.Identity+ADs-
using Microsoft.AspNetCore.Mvc+ADs-
using Microsoft.AspNetCore.Mvc.RazorPages+ADs-
using System.ComponentModel.DataAnnotations+ADs-
using System.Threading.Tasks+ADs-
using RESUMATE+AF8-FINAL+AF8-WORKING+AF8-MODEL.Data+ADs-
using RESUMATE+AF8-FINAL+AF8-WORKING+AF8-MODEL.Models+ADs-
using Microsoft.AspNetCore.Hosting+ADs-
using System.IO+ADs-
using Microsoft.AspNetCore.Http+ADs-
using System+ADs-

namespace RESUMATE+AF8-FINAL+AF8-WORKING+AF8-MODEL.Pages
+AHs-
    public class ApplicantSignupModel : PageModel
    +AHs-
        private readonly UserManager+ADw-IdentityUser+AD4- +AF8-userManager+ADs-
        private readonly SignInManager+ADw-IdentityUser+AD4- +AF8-signInManager+ADs-
        private readonly AppDbContext +AF8-context+ADs-
        private readonly IWebHostEnvironment +AF8-environment+ADs-
        private readonly ILogger+ADw-ApplicantSignupModel+AD4- +AF8-logger+ADs-

        public ApplicantSignupModel(
            UserManager+ADw-IdentityUser+AD4- userManager,
            SignInManager+ADw-IdentityUser+AD4- signInManager,
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger+ADw-ApplicantSignupModel+AD4- logger)
        +AHs-
            +AF8-userManager +AD0- userManager+ADs-
            +AF8-signInManager +AD0- signInManager+ADs-
            +AF8-context +AD0- context+ADs-
            +AF8-environment +AD0- environment+ADs-
            +AF8-logger +AD0- logger+ADs-
        +AH0-

        +AFs-BindProperty+AF0-
        public InputModel Input +AHs- get+ADs- set+ADs- +AH0- +AD0- new InputModel()+ADs-

        public class InputModel
        +AHs-
            +AFs-Required(ErrorMessage +AD0- +ACI-Email is required+ACI-)+AF0-
            +AFs-EmailAddress(ErrorMessage +AD0- +ACI-Invalid email address+ACI-)+AF0-
            public string Email +AHs- get+ADs- set+ADs- +AH0- +AD0- string.Empty+ADs-

            +AFs-Required(ErrorMessage +AD0- +ACI-Password is required+ACI-)+AF0-
            +AFs-DataType(DataType.Password)+AF0-
            +AFs-StringLength(100, ErrorMessage +AD0- +ACI-Password must be at least 6 characters long+ACI-, MinimumLength +AD0- 6)+AF0-
            public string Password +AHs- get+ADs- set+ADs- +AH0- +AD0- string.Empty+ADs-

            +AFs-Required(ErrorMessage +AD0- +ACI-Full name is required+ACI-)+AF0-
            +AFs-StringLength(100, ErrorMessage +AD0- +ACI-Full name cannot exceed 100 characters+ACI-)+AF0-
            public string FullName +AHs- get+ADs- set+ADs- +AH0- +AD0- string.Empty+ADs-

            +AFs-Required(ErrorMessage +AD0- +ACI-Date of birth is required+ACI-)+AF0-
            +AFs-DataType(DataType.Date)+AF0-
            +AFs-Range(typeof(DateTime), +ACI-1/1/1900+ACI-, +ACI-1/1/2100+ACI-, ErrorMessage +AD0- +ACI-Please enter a valid date+ACI-)+AF0-
            public DateTime DateOfBirth +AHs- get+ADs- set+ADs- +AH0- +AD0- DateTime.Now.AddYears(-18)+ADs-

            +AFs-Required(ErrorMessage +AD0- +ACI-Phone number is required+ACI-)+AF0-
            +AFs-Phone(ErrorMessage +AD0- +ACI-Invalid phone number+ACI-)+AF0-
            +AFs-StringLength(15, ErrorMessage +AD0- +ACI-Phone number cannot exceed 15 characters+ACI-)+AF0-
            public string PhoneNumber +AHs- get+ADs- set+ADs- +AH0- +AD0- string.Empty+ADs-

            +AFs-StringLength(200, ErrorMessage +AD0- +ACI-Address cannot exceed 200 characters+ACI-)+AF0-
            public string Address +AHs- get+ADs- set+ADs- +AH0- +AD0- string.Empty+ADs-

            +AFs-StringLength(50, ErrorMessage +AD0- +ACI-City cannot exceed 50 characters+ACI-)+AF0-
            public string City +AHs- get+ADs- set+ADs- +AH0- +AD0- string.Empty+ADs-

            +AFs-StringLength(10, ErrorMessage +AD0- +ACI-Pincode cannot exceed 10 characters+ACI-)+AF0-
            public string Pincode +AHs- get+ADs- set+ADs- +AH0- +AD0- string.Empty+ADs-

            +AFs-FileExtensions(Extensions +AD0- +ACI-jpg,jpeg,png,gif+ACI-, ErrorMessage +AD0- +ACI-Please upload a valid image file (JPG, PNG, GIF)+ACI-)+AF0-
            public IFormFile? ProfilePhoto +AHs- get+ADs- set+ADs- +AH0-

            +AFs-Range(typeof(bool), +ACI-true+ACI-, +ACI-true+ACI-, ErrorMessage +AD0- +ACI-You must accept the terms and conditions+ACI-)+AF0-
            public bool TermsAccepted +AHs- get+ADs- set+ADs- +AH0-
        +AH0-

        public void OnGet()
        +AHs-
            // Initialize with default values
            Input ??+AD0- new InputModel()+ADs-
        +AH0-

        public async Task+ADw-IActionResult+AD4- OnPostAsync()
        +AHs-
            +AF8-logger.LogInformation(+ACIAPQA9AD0- REGISTRATION PROCESS STARTED +AD0APQA9ACI-)+ADs-

            if (+ACE-ModelState.IsValid)
            +AHs-
                +AF8-logger.LogWarning(+ACI-Model state invalid - showing validation errors+ACI-)+ADs-
                foreach (var error in ModelState)
                +AHs-
                    if (error.Value?.Errors?.Count +AD4- 0)
                    +AHs-
                        +AF8-logger.LogWarning(+ACI-Field: +AHs-Field+AH0-, Error: +AHs-Error+AH0AIg-,
                            error.Key, error.Value.Errors+AFs-0+AF0-.ErrorMessage)+ADs-
                    +AH0-
                +AH0-
                return Page()+ADs-
            +AH0-

            +AF8-logger.LogInformation(+ACI-Model state valid - processing registration for: +AHs-Email+AH0AIg-, Input.Email)+ADs-

            IdentityUser? user +AD0- null+ADs-
            try
            +AHs-
                // Check if email already exists
                +AF8-logger.LogInformation(+ACI-Checking for existing user with email: +AHs-Email+AH0AIg-, Input.Email)+ADs-
                var existingUser +AD0- await +AF8-userManager.FindByEmailAsync(Input.Email)+ADs-
                if (existingUser +ACEAPQ- null)
                +AHs-
                    +AF8-logger.LogWarning(+ACI-Email already exists: +AHs-Email+AH0AIg-, Input.Email)+ADs-
                    ModelState.AddModelError(nameof(Input.Email), +ACI-This email is already registered+ACI-)+ADs-
                    return Page()+ADs-
                +AH0-

                // Step 1: Create Identity User
                +AF8-logger.LogInformation(+ACI-Creating Identity user...+ACI-)+ADs-
                user +AD0- new IdentityUser
                +AHs-
                    UserName +AD0- Input.Email,
                    Email +AD0- Input.Email,
                    EmailConfirmed +AD0- true // Set to true for testing
                +AH0AOw-

                +AF8-logger.LogInformation(+ACI-Calling UserManager.CreateAsync...+ACI-)+ADs-
                var createResult +AD0- await +AF8-userManager.CreateAsync(user, Input.Password)+ADs-

                if (+ACE-createResult.Succeeded)
                +AHs-
                    +AF8-logger.LogError(+ACI-USER CREATION FAILED+ACI-)+ADs-
                    foreach (var error in createResult.Errors)
                    +AHs-
                        +AF8-logger.LogError(+ACI-Identity error: +AHs-Code+AH0- - +AHs-Description+AH0AIg-, error.Code, error.Description)+ADs-
                        ModelState.AddModelError(string.Empty, error.Description)+ADs-
                    +AH0-
                    return Page()+ADs-
                +AH0-

                +AF8-logger.LogInformation(+ACInEw- User created successfully with ID: +AHs-UserId+AH0AIg-, user.Id)+ADs-

                // Step 2: Create Applicant Profile - Using fully qualified name to avoid namespace conflict
                +AF8-logger.LogInformation(+ACI-Creating Applicant profile...+ACI-)+ADs-
                var applicant +AD0- new RESUMATE+AF8-FINAL+AF8-WORKING+AF8-MODEL.Models.Applicant
                +AHs-
                    UserId +AD0- user.Id,
                    Email +AD0- Input.Email,
                    FullName +AD0- Input.FullName.Trim(),
                    DateOfBirth +AD0- Input.DateOfBirth.Date,
                    PhoneNumber +AD0- Input.PhoneNumber.Trim(),
                    Address +AD0- Input.Address?.Trim(),
                    City +AD0- Input.City?.Trim(),
                    Pincode +AD0- Input.Pincode?.Trim(),
                    ProfilePhotoPath +AD0- null, // Skip photo for testing
                    CreatedAt +AD0- DateTime.UtcNow,
                    UpdatedAt +AD0- DateTime.UtcNow,
                    IsActive +AD0- true,
                    IsEmailVerified +AD0- true,
                    IsProfileComplete +AD0- false
                +AH0AOw-

                +AF8-logger.LogInformation(+ACI-Adding applicant to context...+ACI-)+ADs-
                +AF8-context.Applicants.Add(applicant)+ADs-

                +AF8-logger.LogInformation(+ACI-Calling SaveChangesAsync...+ACI-)+ADs-
                var recordsSaved +AD0- await +AF8-context.SaveChangesAsync()+ADs-

                +AF8-logger.LogInformation(+ACInEw- SaveChangesAsync completed. Records saved: +AHs-RecordsSaved+AH0AIg-, recordsSaved)+ADs-
                +AF8-logger.LogInformation(+ACInEw- Applicant profile created with ID: +AHs-ApplicantId+AH0AIg-, applicant.Id)+ADs-

                // Step 3: Add to Applicant role
                +AF8-logger.LogInformation(+ACI-Adding user to Applicant role...+ACI-)+ADs-
                var roleResult +AD0- await +AF8-userManager.AddToRoleAsync(user, +ACI-Applicant+ACI-)+ADs-
                if (roleResult.Succeeded)
                +AHs-
                    +AF8-logger.LogInformation(+ACInEw- User added to Applicant role successfully+ACI-)+ADs-
                +AH0-
                else
                +AHs-
                    +AF8-logger.LogWarning(+ACI-Failed to add user to Applicant role, but continuing...+ACI-)+ADs-
                +AH0-

                // Step 4: Sign in user
                +AF8-logger.LogInformation(+ACI-Signing in user...+ACI-)+ADs-
                await +AF8-signInManager.SignInAsync(user, isPersistent: false)+ADs-
                +AF8-logger.LogInformation(+ACInEw- User signed in successfully+ACI-)+ADs-

                TempData+AFsAIg-SuccessMessage+ACIAXQ- +AD0- +ACQAIg-Registration successful+ACE- Welcome +AHs-Input.FullName+AH0AIgA7-
                +AF8-logger.LogInformation(+ACIAPQA9AD0- REGISTRATION PROCESS COMPLETED SUCCESSFULLY +AD0APQA9ACI-)+ADs-

                return RedirectToPage(+ACI-/Index+ACI-)+ADs-
            +AH0-
            catch (Exception ex)
            +AHs-
                +AF8-logger.LogError(ex, +ACInTA- CRITICAL ERROR during registration process+ACI-)+ADs-

                // Cleanup if user was created but applicant profile failed
                if (user +ACEAPQ- null)
                +AHs-
                    try
                    +AHs-
                        +AF8-logger.LogInformation(+ACI-Cleaning up partially created user...+ACI-)+ADs-
                        await +AF8-userManager.DeleteAsync(user)+ADs-
                        +AF8-logger.LogInformation(+ACI-User cleanup completed+ACI-)+ADs-
                    +AH0-
                    catch (Exception cleanupEx)
                    +AHs-
                        +AF8-logger.LogError(cleanupEx, +ACI-Failed to cleanup user after registration error+ACI-)+ADs-
                    +AH0-
                +AH0-

                ModelState.AddModelError(string.Empty,
                    +ACI-A system error occurred during registration. Please try again.+ACI-)+ADs-
                return Page()+ADs-
            +AH0-
        +AH0-
    +AH0-
+AH0-