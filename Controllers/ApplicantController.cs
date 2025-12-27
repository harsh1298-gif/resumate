using Microsoft.AspNetCore.Mvc;
using RESUMATE_FINAL_WORKING_MODEL.Models;
using System.Diagnostics; // Add this for debugging

namespace RESUMATE_FINAL_WORKING_MODEL.Controllers
{
    public class ApplicantController : Controller
    {
        // GET: /Applicant/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Applicant/Login  
        [HttpPost]
        public IActionResult Login(LoginModel model) // You'll need a LoginModel class
        {
            if (ModelState.IsValid)
            {
                // TODO: Add your authentication logic here
                // Check if username/password are correct

                // For now, just redirect to Dashboard
                Debug.WriteLine("Login successful - redirecting to dashboard");
                return RedirectToAction("Dashboard", "Applicant");
            }

            // If we got this far, something failed
            return View(model);
        }

        // Add Dashboard action
        public IActionResult Dashboard()
        {
            return View();
        }

        // Your existing Signup methods below...
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Signup(Applicant applicant)
        {
            if (ModelState.IsValid)
            {
                // For now, just redirect to success
                return RedirectToAction("Success");
            }
            return View(applicant);
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}