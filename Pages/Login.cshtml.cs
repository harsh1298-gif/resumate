using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication; // Add this using directive

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReturnUrl { get; set; } = string.Empty;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
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
                // FIXED: Use the correct SignOutAsync method
                await _signInManager.SignOutAsync();

                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully.", Input.Email);

                    // Get user and check roles for proper redirection
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        if (await _userManager.IsInRoleAsync(user, "Recruiter"))
                        {
                            return RedirectToPage("/Recruiter/Dashboard");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Applicant"))
                        {
                            return RedirectToPage("/Applicant/Dashboard");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToPage("/Admin/Dashboard");
                        }
                    }

                    // Fallback to returnUrl if no role match
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account {Email} locked out.", Input.Email);
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                    _logger.LogWarning("Invalid login attempt for user {Email}.", Input.Email);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for user {Email}.", Input.Email);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return Page();
            }
        }

        // Optional: Demo login method for testing
        public async Task<IActionResult> OnPostDemoLoginAsync(string role = "Applicant")
        {
            string demoEmail = "";
            string demoPassword = "Demo@123";

            switch (role.ToLower())
            {
                case "admin":
                    demoEmail = "admin@resumate.com";
                    break;
                case "recruiter":
                    demoEmail = "recruiter@resumate.com";
                    break;
                case "applicant":
                default:
                    demoEmail = "applicant@resumate.com";
                    break;
            }

            var result = await _signInManager.PasswordSignInAsync(demoEmail, demoPassword, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Demo user {Email} logged in successfully.", demoEmail);
                return RedirectToPage("/Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Demo login failed. Please ensure demo users are seeded in the database.");
                return Page();
            }
        }
    }
}
