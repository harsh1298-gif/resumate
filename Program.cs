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

            // Configure cookie settings
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.LoginPath = "/ApplicantSignup";
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
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    }
                };
            });

            // 4. Add health checks with database check
            builder.Services.AddHealthChecks()
                .AddSqlServer(connectionString);

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
            });

            // Add logging
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            });

            // Add services to the container.
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

                    // More flexible CSP for production
                    headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://cdnjs.cloudflare.com; " +
                        "style-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://cdnjs.cloudflare.com; " +
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
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Cache static files for 30 days
                    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=2592000";
                    ctx.Context.Response.Headers["ETag"] = $"\"{DateTime.UtcNow.Ticks}\"";
                }
            });

            app.UseRouting();

            // Add security headers
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // 5. Enhanced database seeding with better error handling
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var services = scope.ServiceProvider;
                var scopeLogger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    scopeLogger.LogInformation("Starting database initialization...");

                    var context = services.GetRequiredService<AppDbContext>();

                    // Check database connection first
                    scopeLogger.LogInformation("Testing database connection...");
                    if (!await context.Database.CanConnectAsync())
                    {
                        scopeLogger.LogWarning("Cannot connect to database. Skipping migrations.");
                    }
                    else
                    {
                        // Use Migrate instead of EnsureCreated for better control
                        scopeLogger.LogInformation("Applying pending migrations...");
                        await context.Database.MigrateAsync();
                        scopeLogger.LogInformation("Migrations applied successfully");

                        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

                        // Create roles if they don't exist
                        string[] roleNames = { "Applicant", "Recruiter", "Admin" };
                        foreach (var roleName in roleNames)
                        {
                            if (!await roleManager.RoleExistsAsync(roleName))
                            {
                                await roleManager.CreateAsync(new IdentityRole(roleName));
                                scopeLogger.LogInformation("Created role: {RoleName}", roleName);
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

                            var createAdmin = await userManager.CreateAsync(admin, "Admin12345!");
                            if (createAdmin.Succeeded)
                            {
                                await userManager.AddToRoleAsync(admin, "Admin");
                                scopeLogger.LogInformation("Created default admin user: {Email}", adminEmail);
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
                        }

                        scopeLogger.LogInformation("Database initialization completed successfully");
                    }
                }
                catch (Exception ex)
                {
                    scopeLogger.LogError(ex, "An error occurred while seeding the database");
                    // Don't rethrow - allow app to start even if DB is unavailable
                    scopeLogger.LogWarning("Application will continue without database initialization");
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
                            duration = e.Value.Duration
                        }),
                        totalDuration = report.TotalDuration
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            });

            // Enhanced database test endpoint
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
                            appliedMigrations = appliedMigrations.ToArray()
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
                        details = ex.ToString(),
                        timestamp = DateTime.UtcNow
                    }, statusCode: 500);
                }
            });

            app.MapRazorPages();

            // Add a simple status endpoint
            app.MapGet("/api/status", () => new { status = "OK", timestamp = DateTime.UtcNow });

            logger.LogInformation("Application startup completed - ready to accept requests");

            await app.RunAsync();
        }
    }
}