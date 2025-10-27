using Microsoft.AspNetCore.Mvc;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Controllers
{
    public class CompanyController : Controller
    {
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Signup(Company company)
        {
            if (ModelState.IsValid)
            {
                // For now, just redirect to success
                return RedirectToAction("Success");
            }
            return View(company);
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}