using Microsoft.AspNetCore.Mvc;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Controllers
{
    public class ApplicantController : Controller
    {
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