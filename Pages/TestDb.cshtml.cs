using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RESUMATE_FINAL_WORKING_MODEL.Data;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    public class TestDbModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TestDbModel> _logger;

        public TestDbModel(AppDbContext context, ILogger<TestDbModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string ConnectionStatus { get; set; }
        public string ApplicantsCount { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> DatabaseDetails { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Test 1: Basic connection
                var canConnect = await _context.Database.CanConnectAsync();
                ConnectionStatus = canConnect ? "? Database Connection Successful" : "? Database Connection Failed";

                if (canConnect)
                {
                    // Test 2: Check if Applicants table exists and get count
                    try
                    {
                        var count = await _context.Applicants.CountAsync();
                        ApplicantsCount = $"? Applicants table exists with {count} records";
                    }
                    catch (Exception ex)
                    {
                        ApplicantsCount = $"? Applicants table error: {ex.Message}";
                    }

                    // Test 3: Get database info
                    var dbName = _context.Database.GetDbConnection().Database;
                    var dataSource = _context.Database.GetDbConnection().DataSource;

                    DatabaseDetails.Add($"Database: {dbName}");
                    DatabaseDetails.Add($"Server: {dataSource}");
                    DatabaseDetails.Add($"Connection State: {_context.Database.GetDbConnection().State}");

                    // Test 4: Try to read first applicant (if exists)
                    try
                    {
                        var firstApplicant = await _context.Applicants.FirstOrDefaultAsync();
                        if (firstApplicant != null)
                        {
                            DatabaseDetails.Add($"? Sample Applicant: {firstApplicant.FullName} ({firstApplicant.Email})");
                        }
                        else
                        {
                            DatabaseDetails.Add("?? No applicants in database yet");
                        }
                    }
                    catch (Exception ex)
                    {
                        DatabaseDetails.Add($"? Error reading applicants: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = "? Database Connection Failed";
                ErrorMessage = ex.Message;
                _logger.LogError(ex, "Database test failed");
            }
        }
    }
}
