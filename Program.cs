using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;
using RESUMATE_FINAL_WORKING_MODEL.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http.Features;
using System.IO;

namespace RESUMATE_FINAL_WORKING_MODEL
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Get the database connection string
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Log connection string (without password for security)
            var safeConnectionString = new SqlConnectionStringBuilder(connectionString);
            safeConnectionString.Password = "***";
            Console.WriteLine($"Database: {safeConnectionString.DataSource}, Initial Catalog: {safeConnectionString.InitialCatalog}");

            // 2. Configure DbContext with retry policy
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        sqlServerOptions.CommandTimeout(60);
                        sqlServerOptions.MigrationsAssembly("RESUMATE_FINAL_WORKING_MODEL");
                    });

                // Enable sensitive data logging only in development
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }
            });

            // 3. Add and configure ASP.NET Core Identity with Roles
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                // Relaxed settings for development
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                // Password settings (relaxed for development)
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Configure form options for file uploads
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 10485760; // 10MB limit
                options.ValueLengthLimit = 10485760;
                options.MemoryBufferThreshold = 1024 * 1024; // 1MB
            });

            // Configure cookie settings
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/Error";
                options.SlidingExpiration = true;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        // For API calls, return 401 instead of redirect
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = 403;
                            return Task.CompletedTask;
                        }
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            });

            // 4. Add health checks with database check
            builder.Services.AddHealthChecks()
                .AddSqlServer(connectionString, timeout: TimeSpan.FromSeconds(10));

            // Add response compression
            builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = new[]
                {
                    "text/plain",
                    "text/css",
                    "application/javascript",
                    "text/html",
                    "application/xml",
                    "text/xml",
                    "application/json",
                    "text/json",
                    "image/svg+xml"
                };
            });

            // Add request localization
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { "en-US" };
                options.SetDefaultCulture(supportedCultures[0])
                       .AddSupportedCultures(supportedCultures)
                       .AddSupportedUICultures(supportedCultures);
            });

            // Add HTTP context accessor
            builder.Services.AddHttpContextAccessor();

            // Add response caching
            builder.Services.AddResponseCaching(options =>
            {
                options.MaximumBodySize = 1024 * 1024;
                options.UseCaseSensitivePaths = true;
            });

            // Add Antiforgery service
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.SuppressXFrameOptionsHeader = false;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
            });

            // Add logging
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                if (!builder.Environment.IsDevelopment())
                {
                    logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                }
            });

            // Add services to the container
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Get logger for application startup
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Application starting...");
            logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Development security headers
                app.Use(async (context, next) =>
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                    await next();
                });
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();

                // Full security headers in production
                app.Use(async (context, next) =>
                {
                    var headers = context.Response.Headers;
                    headers["X-Content-Type-Options"] = "nosniff";
                    headers["X-Frame-Options"] = "DENY";
                    headers["X-XSS-Protection"] = "1; mode=block";
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

                    // More flexible CSP for production
                    headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                        "style-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self' https://cdnjs.cloudflare.com https://fonts.gstatic.com; " +
                        "connect-src 'self';";

                    await next();
                });
            }

            // Enable response compression
            app.UseResponseCompression();

            // Request localization
            var supportedCultures = new[] { new CultureInfo("en-US") };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            // Enable response caching
            app.UseResponseCaching();

            app.UseHttpsRedirection();

            // Configure static files with proper caching
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    var path = ctx.Context.Request.Path.Value ?? string.Empty;

                    // Don't cache uploaded files
                    if (path.StartsWith("/uploads/"))
                    {
                        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                        ctx.Context.Response.Headers["Pragma"] = "no-cache";
                        ctx.Context.Response.Headers["Expires"] = "0";
                    }
                    else
                    {
                        // Cache static assets for 30 days
                        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=2592000";
                    }
                }
            });

            app.UseRouting();

            // Add security headers for forwarded requests
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // 5. Enhanced database seeding with better error handling
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var scopeLogger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    scopeLogger.LogInformation("Starting database initialization...");

                    var dbContext = services.GetRequiredService<AppDbContext>();

                    // Log the actual connection string being used
                    var actualConnectionString = dbContext.Database.GetConnectionString();
                    scopeLogger.LogInformation($"Actual connection string from DbContext: {actualConnectionString}");

                    // Test if we can connect - with detailed error info
                    scopeLogger.LogInformation("Testing database connection...");

                    try
                    {
                        var canConnect = await dbContext.Database.CanConnectAsync();
                        scopeLogger.LogInformation($"Can connect to database: {canConnect}");

                        if (!canConnect)
                        {
                            scopeLogger.LogError("Cannot connect to database! Connection test returned false.");
                            var connBuilder = new SqlConnectionStringBuilder(actualConnectionString);
                            scopeLogger.LogError($"Server: {connBuilder.DataSource}");
                            scopeLogger.LogError($"Database: {connBuilder.InitialCatalog}");
                        }
                    }
                    catch (Exception connEx)
                    {
                        scopeLogger.LogError(connEx, "Connection test threw an exception!");
                        scopeLogger.LogError($"Error: {connEx.Message}");
                        if (connEx.InnerException != null)
                        {
                            scopeLogger.LogError($"Inner Error: {connEx.InnerException.Message}");
                        }
                        throw;
                    }

                    // NOTE: Database drop disabled - data will persist between restarts
                    // Uncomment below ONLY if you need to reset the database completely
                    // if (builder.Environment.IsDevelopment())
                    // {
                    //     scopeLogger.LogInformation("Dropping existing database (if exists)...");
                    //     await dbContext.Database.EnsureDeletedAsync();
                    //     scopeLogger.LogInformation("Database dropped successfully");
                    // }

                    // Apply all pending migrations
                    scopeLogger.LogInformation("Applying pending migrations...");
                    await dbContext.Database.MigrateAsync();
                    scopeLogger.LogInformation("Migrations applied successfully");

                    // Create uploads directory if it doesn't exist
                    var webRootPath = builder.Environment.WebRootPath;
                    var uploadsPath = Path.Combine(webRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                        scopeLogger.LogInformation("Created uploads directory: {UploadsPath}", uploadsPath);
                    }
                    else
                    {
                        scopeLogger.LogInformation("Uploads directory already exists: {UploadsPath}", uploadsPath);
                    }

                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

                    // Create roles if they don't exist
                    string[] roleNames = { "Applicant", "Recruiter", "Admin" };
                    foreach (var roleName in roleNames)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                            if (roleResult.Succeeded)
                            {
                                scopeLogger.LogInformation("Created role: {RoleName}", roleName);
                            }
                            else
                            {
                                scopeLogger.LogWarning("Failed to create role {RoleName}: {Errors}",
                                    roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            }
                        }
                        else
                        {
                            scopeLogger.LogInformation("Role already exists: {RoleName}", roleName);
                        }
                    }

                    // Create default admin user if not exists
                    var adminEmail = "admin@resumate.com";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    if (adminUser == null)
                    {
                        var admin = new IdentityUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true
                        };

                        var createAdmin = await userManager.CreateAsync(admin, "Admin@12345");
                        if (createAdmin.Succeeded)
                        {
                            await userManager.AddToRoleAsync(admin, "Admin");
                            scopeLogger.LogInformation("Created default admin user: {Email}", adminEmail);
                            scopeLogger.LogInformation("Admin credentials - Email: {Email}, Password: Admin@12345", adminEmail);
                        }
                        else
                        {
                            scopeLogger.LogWarning("Failed to create admin user: {Errors}",
                                string.Join(", ", createAdmin.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        scopeLogger.LogInformation("Admin user already exists: {Email}", adminEmail);

                        // Ensure admin has the Admin role
                        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                            scopeLogger.LogInformation("Added Admin role to existing admin user");
                        }
                    }

                    scopeLogger.LogInformation("Database initialization completed successfully");
                }
                catch (Exception ex)
                {
                    scopeLogger.LogError(ex, "An error occurred while seeding the database");
                    // Don't throw in production - let app start even if seeding fails
                    if (builder.Environment.IsDevelopment())
                    {
                        throw;
                    }
                }
            }

            // Map health check endpoint
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            exception = e.Value.Exception?.Message,
                            duration = e.Value.Duration.TotalMilliseconds
                        }),
                        totalDuration = report.TotalDuration.TotalMilliseconds
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                }
            });

            // Enhanced database test endpoint (only in development)
            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/api/debug/database", async (AppDbContext context) =>
                {
                    try
                    {
                        var canConnect = await context.Database.CanConnectAsync();

                        int applicantsCount = 0;
                        int usersCount = 0;

                        try
                        {
                            applicantsCount = await context.Applicants.CountAsync();
                        }
                        catch
                        {
                            applicantsCount = -1;
                        }

                        try
                        {
                            usersCount = await context.Users.CountAsync();
                        }
                        catch
                        {
                            usersCount = -1;
                        }

                        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

                        return Results.Json(new
                        {
                            status = "success",
                            database = new
                            {
                                canConnect,
                                applicantsCount,
                                usersCount,
                                pendingMigrations = pendingMigrations.ToArray(),
                                appliedMigrations = appliedMigrations.ToArray(),
                                connectionString = context.Database.GetConnectionString()?.Split(';').FirstOrDefault()
                            },
                            timestamp = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        return Results.Json(new
                        {
                            status = "error",
                            error = ex.Message,
                            innerException = ex.InnerException?.Message,
                            stackTrace = ex.StackTrace,
                            timestamp = DateTime.UtcNow
                        }, statusCode: 500);
                    }
                });
            }

            app.MapRazorPages();

            // Add a simple status endpoint
            app.MapGet("/api/status", () => new
            {
                status = "OK",
                environment = app.Environment.EnvironmentName,
                timestamp = DateTime.UtcNow
            });

            logger.LogInformation("Application startup completed - ready to accept requests");
            logger.LogInformation("Navigate to /ApplicantSignup to create an account");
            logger.LogInformation("Health check available at /health");

            await app.RunAsync();
        }
    }
}