using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    
    public class ViewDataModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ViewDataModel> _logger;

        public ViewDataModel(AppDbContext context, ILogger<ViewDataModel> logger)
        {
            _context = context;
            _logger = logger;
            Applicants = new List<Models.Applicant>();
        }

        public IList<Models.Applicant> Applicants { get; set; }
        public bool HasData => Applicants != null && Applicants.Count > 0;

        private async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                
                // Test connection
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation($"Database connection test: {canConnect}");

                // Test basic query
                var count = await _context.Applicants.CountAsync();
                _logger.LogInformation($"Found {count} applicants in the database.");

                // Test insert
                var testEmail = $"test-{Guid.NewGuid()}@test.com";
                var testApplicant = new Models.Applicant
                {
                    FullName = "Test User",
                    Email = testEmail,
                    DateOfBirth = new DateTime(1990, 1, 1),
                    PhoneNumber = "1234567890"
                };

                _context.Applicants.Add(testApplicant);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully inserted test record");

                // Verify the record was inserted
                var inserted = await _context.Applicants
                    .FirstOrDefaultAsync(a => a.Email == testEmail);
                
                if (inserted != null)
                {
                    _logger.LogInformation($"Successfully retrieved test record with ID: {inserted.Id}");
                    // Clean up
                    _context.Applicants.Remove(inserted);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up test record");
                }
                else
                {
                    _logger.LogError("Failed to retrieve the test record after insertion");
                }

                return Content($"Database test completed. Check logs for details. Found {count} applicants.\n" +
                             $"Test record {(inserted != null ? "was inserted and verified successfully" : "failed to insert")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                return Content($"Error testing database: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public async Task<IActionResult> OnGetAsync(bool test = false)
        {
            if (test)
            {
                return await TestDatabaseConnection();
            }

            try
            {
                _logger.LogInformation("Attempting to retrieve applicant data...");
                
                // Check if we can connect to the database
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogError("Cannot connect to the database.");
                    return Page();
                }

                // Check if the Applicants table exists
                var tableExists = await _context.Database.ExecuteSqlRawAsync(@"
                    SELECT 1 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = 'dbo' 
                    AND TABLE_NAME = 'Applicants'") > 0;

                if (!tableExists)
                {
                    Console.WriteLine("Applicants table does not exist.");
                    return Page();
                }

                // Get the count of applicants
                var count = await _context.Applicants.CountAsync();
                Console.WriteLine($"Found {count} applicants in the database.");

                // Get all applicants with related data if needed
                Applicants = await _context.Applicants
                    .OrderByDescending(a => a.Id)
                    .Take(100) // Limit to 100 records for performance
                    .ToListAsync();

                Console.WriteLine($"Successfully loaded {Applicants.Count} applicants.");
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnGetAsync: {ex}");
                // Ensure we have an empty list on error
                Applicants = new List<Models.Applicant>();
                return Page();
            }
        }
    }
}
