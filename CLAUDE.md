# CLAUDE.md - ResuMate Project Guide

This document provides essential context for AI assistants working with the ResuMate codebase.

## Project Overview

**ResuMate** is an AI-powered recruitment platform that connects job seekers with employers through intelligent matching. The platform serves three user roles:
- **Applicants**: Job seekers creating profiles and applying to positions
- **Recruiters**: Hiring managers posting jobs and managing applications
- **Admins**: System administrators managing the platform

## Technology Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core 9.0 with Razor Pages |
| Language | C# with nullable reference types |
| Database | SQL Server (LocalDB) |
| ORM | Entity Framework Core 9.0.10 |
| Authentication | ASP.NET Identity with role-based access |
| Password Hashing | BCrypt.Net-Next 4.0.3 |
| Frontend | Tailwind CSS (CDN), jQuery, FontAwesome |

## Quick Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Run with specific profile
dotnet run --launch-profile "ResuMate"

# Apply database migrations
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName>

# Check health endpoint
curl https://localhost:7001/health
```

## Project Structure

```
resumate/
├── Controllers/           # API controllers (minimal usage)
│   ├── ApplicantController.cs
│   └── CompanyController.cs
├── Data/                  # Data access layer
│   └── AppDbContext.cs    # EF Core DbContext with 8 DbSets
├── Migrations/            # EF Core database migrations
├── Models/                # Entity models
│   ├── Applicant.cs       # Job seeker profile
│   ├── Recruiter.cs       # Hiring manager profile
│   ├── Company.cs         # Employer company
│   ├── Job.cs             # Job posting
│   ├── Application.cs     # Job application
│   ├── Experience.cs      # Work experience
│   ├── Education.cs       # Educational background
│   ├── Skill.cs           # Skill taxonomy
│   ├── ApplicantSkill.cs  # M-M junction table
│   └── JobRequirement.cs  # M-M junction table
├── Pages/                 # Razor Pages (primary UI)
│   ├── Index.cshtml       # Landing page
│   ├── Login.cshtml       # Authentication
│   ├── ApplicantSignup.cshtml  # Applicant registration
│   ├── RecruiterSignup.cshtml  # Recruiter registration
│   ├── SignupRole.cshtml  # Role selection
│   ├── Applicant/         # Applicant-specific pages
│   │   └── Dashboard.cshtml
│   ├── Shared/            # Shared partials
│   └── _Layout.cshtml     # Master layout
├── Properties/
│   └── launchSettings.json
├── wwwroot/               # Static files
│   ├── css/
│   ├── js/
│   ├── lib/               # Client libraries (jQuery)
│   └── images/
├── Program.cs             # Application entry point
├── appsettings.json       # Configuration
└── *.csproj               # Project file
```

## Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | Application startup, DI configuration, middleware pipeline, database seeding |
| `Data/AppDbContext.cs` | Entity Framework DbContext with all DbSets |
| `Pages/_Layout.cshtml` | Master layout with navigation and Tailwind styling |
| `appsettings.json` | Connection strings and logging configuration |

## Architecture Patterns

### Razor Pages Pattern
This project uses **Razor Pages** (not MVC). Each page has:
- `.cshtml` file for the view
- `.cshtml.cs` file for the PageModel with handlers

```csharp
// Handler methods follow this pattern:
public async Task<IActionResult> OnGetAsync() { }
public async Task<IActionResult> OnPostAsync() { }
```

### Authorization
Pages are protected using role-based authorization:
```csharp
[Authorize(Roles = "Applicant")]
public class DashboardModel : PageModel { }
```

### Data Binding
Use `[BindProperty]` for form binding:
```csharp
[BindProperty]
public Applicant Applicant { get; set; }
```

## Database Schema

### Core Entities
- **Applicant** - Job seeker with profile, resume, skills, experience, education
- **Recruiter** - Hiring manager with permissions and company association
- **Company** - Employer with industry, size, subscription plan
- **Job** - Job posting with type, category, experience level
- **Application** - Junction between Applicant and Job with status

### Roles
Three predefined roles (seeded at startup):
- `Applicant`
- `Recruiter`
- `Admin`

### Default Admin
Seeded at startup: `admin@resumate.com` / `Admin12345!`

## Code Conventions

### Naming
- **PascalCase**: Classes, methods, properties, enum values
- **camelCase**: Local variables, parameters
- **_prefix**: Private fields (e.g., `_signInManager`)

### Entity Framework
- Use data annotations for validation: `[Required]`, `[EmailAddress]`, `[StringLength]`
- Use `[Column("name")]` for custom column mappings
- Use `[NotMapped]` for computed properties

### Async/Await
All database operations should be async:
```csharp
var applicant = await _context.Applicants.FindAsync(id);
await _context.SaveChangesAsync();
```

### Validation
- Server-side: ModelState validation with data annotations
- Client-side: jQuery Validation Unobtrusive

## Development URLs

| Profile | HTTPS | HTTP |
|---------|-------|------|
| ResuMate (default) | https://localhost:7001 | http://localhost:5001 |
| Production | https://localhost:7000 | http://localhost:5000 |

## API Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/health` | Health check with database status |
| `/api/status` | Simple status check |
| `/api/debug/database` | Database diagnostics (dev only) |

## File Upload Locations

- Profile photos: `wwwroot/uploads/logos/`
- Resumes: Custom paths stored in `ResumeFilePath`

## Security Configuration

### Password Policy (Development)
- Minimum 6 characters
- No special requirements (digits, uppercase, etc.)

### Cookie Authentication
- 30-day sliding expiration
- HttpOnly cookies
- SameSite=Lax

### Security Headers (Production)
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- Content-Security-Policy configured for CDN resources

## Common Tasks

### Adding a New Page
1. Create `.cshtml` and `.cshtml.cs` in `Pages/`
2. Inherit from `PageModel`
3. Add `[Authorize]` if authentication required

### Adding a New Model
1. Create model class in `Models/`
2. Add DbSet to `AppDbContext.cs`
3. Create migration: `dotnet ef migrations add <Name>`
4. Apply: `dotnet ef database update`

### Adding Authorization to a Page
```csharp
[Authorize(Roles = "Admin")]
public class AdminPageModel : PageModel { }
```

## Testing

Currently no automated test suite. Manual testing available via:
- `/health` - Health check endpoint
- `Pages/DebugDatabase.cshtml` - Database testing
- `Pages/TestDb.cshtml` - Table inspection
- `/api/debug/database` - Database diagnostics

## Dependencies

Key NuGet packages:
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.10
- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.10
- `BCrypt.Net-Next` 4.0.3
- `AspNetCore.HealthChecks.SqlServer` 9.0.0
- `Microsoft.Data.SqlClient` 6.1.2

## Troubleshooting

### Database Connection Issues
1. Ensure LocalDB is running: `sqllocaldb start MSSQLLocalDB`
2. Check connection string in `appsettings.json`
3. Verify migrations applied: `dotnet ef migrations list`

### Build Errors
1. Restore packages: `dotnet restore`
2. Clean and rebuild: `dotnet clean && dotnet build`

### Identity Issues
- Check role seeding in `Program.cs`
- Verify user has correct role assignment
- Check cookie configuration

## Important Notes for AI Assistants

1. **Razor Pages, not MVC**: This uses Razor Pages pattern with PageModels, not Controllers/Views
2. **SQL Server LocalDB**: Database runs on LocalDB, not a full SQL Server instance
3. **No Test Project**: Add integration/unit tests if making significant changes
4. **Namespace**: `RESUMATE_FINAL_WORKING_MODEL` (with underscores)
5. **File Uploads**: Handle file validation carefully for security
6. **Migrations**: Always apply migrations after model changes
