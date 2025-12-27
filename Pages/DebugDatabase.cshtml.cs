using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Pages
{
    [AllowAnonymous]
    public class DebugDatabaseModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public DebugDatabaseModel(
            AppDbContext context, 
            IConfiguration configuration,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
            Logs = new List<string>();
            TableNames = new List<string>();
            Users = new List<IdentityUser>();
            Applicants = new List<Models.Applicant>();
        }

        public bool CanConnect { get; set; }
        public string DatabaseName { get; set; } = "Unknown";
        public List<string> TableNames { get; set; }
        public List<IdentityUser> Users { get; set; }
        public int UserCount => Users?.Count ?? 0;
        public List<Models.Applicant> Applicants { get; set; }
        public int ApplicantCount => Applicants?.Count ?? 0;
        public List<string> Logs { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await CheckDatabaseConnection();
                await LoadTables();
                await LoadUsers();
                await LoadApplicants();
            }
            catch (Exception ex)
            {
                Logs.Add($"ERROR: {ex.Message}");
                Logs.Add(ex.StackTrace);
            }

            return Page();
        }

        private async Task CheckDatabaseConnection()
        {
            try
            {
                CanConnect = await _context.Database.CanConnectAsync();
                if (CanConnect)
                {
                    var connection = _context.Database.GetDbConnection();
                    DatabaseName = connection.Database;
                    Logs.Add($"Successfully connected to database: {DatabaseName}");
                }
                else
                {
                    Logs.Add("Failed to connect to the database.");
                }
            }
            catch (Exception ex)
            {
                Logs.Add($"Error checking database connection: {ex.Message}");
                CanConnect = false;
            }
        }

        private async Task LoadTables()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tables = new List<string>();
                var command = new SqlCommand(
                    "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", 
                    connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }

                TableNames = tables;
                Logs.Add($"Found {tables.Count} tables in the database.");
            }
            catch (Exception ex)
            {
                Logs.Add($"Error loading tables: {ex.Message}");
            }
        }

        private async Task LoadUsers()
        {
            try
            {
                Users = await _userManager.Users.ToListAsync();
                Logs.Add($"Loaded {Users.Count} users from the database.");
            }
            catch (Exception ex)
            {
                Logs.Add($"Error loading users: {ex.Message}");
            }
        }

        private async Task LoadApplicants()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    // Check if table exists
                    var tableExists = TableNames.Contains("Applicants");
                    if (!tableExists)
                    {
                        Logs.Add("Applicants table does not exist in the database.");
                        return;
                    }

                    // Try to load applicants
                    Applicants = await _context.Applicants.ToListAsync();
                    Logs.Add($"Loaded {Applicants.Count} applicants from the database.");

                    // Check for users without corresponding applicants
                    var userIds = Users.Select(u => u.Id).ToHashSet();
                    var applicantUserIds = Applicants.Select(a => a.UserId).ToHashSet();
                    var usersWithoutApplicants = userIds.Except(applicantUserIds).Count();
                    
                    if (usersWithoutApplicants > 0)
                    {
                        Logs.Add($"Warning: Found {usersWithoutApplicants} users without corresponding applicant records.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.Add($"Error loading applicants: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Logs.Add($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}

