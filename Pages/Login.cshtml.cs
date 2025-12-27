using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

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

            ReturnUrl = returnUrl ?? Url.Content("~/");
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
                if (User.Identity?.IsAuthenticated == true)
                {
                    await _signInManager.SignOutAsync();
                }

                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully.", Input.Email);

                    // Get the signed-in user
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        // Check role and redirect accordingly
                        if (await _userManager.IsInRoleAsync(user, "Recruiter"))
                        {
                            _logger.LogInformation("User {Email} redirected to RecruiterDashboard", Input.Email);
                            return RedirectToPage("/RecruiterDashboard");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Applicant"))
                        {
                            _logger.LogInformation("User {Email} redirected to Dashboard (Applicant)", Input.Email);
                            return RedirectToPage("/Dashboard");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            _logger.LogInformation("User {Email} redirected to Index (Admin)", Input.Email);
                            return RedirectToPage("/Index"); // Admin redirect - update if you have AdminDashboard
                        }
                    }

                    // Default fallback to home page if no role found
                    _logger.LogWarning("User {Email} has no recognized role, redirecting to home", Input.Email);
                    return RedirectToPage("/Index");
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
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Login failed.");
                return Page();
            }
        }
    }
}