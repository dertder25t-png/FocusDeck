using FocusDeck.Server.Services;
using FocusDeck.Server.Middleware;
using FocusDeck.Server.HealthChecks;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Jobs;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Hangfire;
using Hangfire.PostgreSql;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "FocusDeck.Server")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting FocusDeck Server");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "FocusDeck.Server")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // Add OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource("FocusDeck.Server")
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FocusDeck.Server"))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                })
                .AddConsoleExporter();
        });

    // Add services to the container.
    builder.Services.AddControllers();
    
    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "FocusDeck API",
            Version = "v1",
            Description = "FocusDeck REST API for productivity management with notes, study sessions, and automations",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "FocusDeck",
                Url = new Uri("https://github.com/dertder25t-png/FocusDeck")
            }
        });

        // Add JWT authentication to Swagger
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the ****** scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Add API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Add HttpClient for OAuth token exchange
    builder.Services.AddHttpClient();

    // Add Database (SQLite as default, PostgreSQL connection string can be configured)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=focusdeck.db";
    
    // Check if PostgreSQL connection string is configured
    if (connectionString.Contains("Host=") || connectionString.Contains("Server="))
    {
        // PostgreSQL
        builder.Services.AddDbContext<AutomationDbContext>(options =>
            options.UseNpgsql(connectionString));
    }
    else
    {
        // SQLite (default)
        builder.Services.AddDbContext<AutomationDbContext>(options =>
            options.UseSqlite(connectionString));
    }

    // Add SharedKernel services
    builder.Services.AddSingleton<IClock, SystemClock>();
    builder.Services.AddSingleton<IIdGenerator, GuidIdGenerator>();

    // Add Auth services
    builder.Services.AddScoped<FocusDeck.Server.Services.Auth.ITokenService, FocusDeck.Server.Services.Auth.TokenService>();

    // Add Storage services
    builder.Services.AddSingleton<FocusDeck.Server.Services.Storage.IAssetStorage, FocusDeck.Server.Services.Storage.LocalFileSystemAssetStorage>();

    // Add Transcription and Text Generation services
    builder.Services.AddSingleton<FocusDeck.Server.Services.Transcription.IWhisperAdapter, FocusDeck.Server.Services.Transcription.StubWhisperAdapter>();
    builder.Services.AddSingleton<FocusDeck.Server.Services.TextGeneration.ITextGen, FocusDeck.Server.Services.TextGeneration.StubTextGen>();

    // Add Job services
    builder.Services.AddScoped<ITranscribeLectureJob, TranscribeLectureJob>();
    builder.Services.AddScoped<ISummarizeLectureJob, SummarizeLectureJob>();
    builder.Services.AddScoped<IVerifyNoteJob, VerifyNoteJob>();
    builder.Services.AddScoped<IGenerateLectureNoteJob, GenerateLectureNoteJob>();

    // Add Sync Service
    builder.Services.AddScoped<ISyncService, SyncService>();

    // Add Action Executor
    builder.Services.AddSingleton<ActionExecutor>();

    // Add server update service
    builder.Services.AddSingleton<IServerUpdateService, ServerUpdateService>();

    // Add background services
    builder.Services.AddHostedService<AutomationEngine>();

    // Add Version Service
    builder.Services.AddSingleton<VersionService>();

    // Add SignalR for real-time notifications
    builder.Services.AddSignalR();

    // Add Hangfire with PostgreSQL storage
    var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=focusdeck.db";
    
    // Only add Hangfire if using PostgreSQL (Hangfire.PostgreSql requires PostgreSQL)
    if (hangfireConnection.Contains("Host=") || hangfireConnection.Contains("Server="))
    {
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection)));

        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5; // Number of concurrent job workers
            options.ServerName = $"FocusDeck-{Environment.MachineName}";
        });
    }
    else
    {
        // For SQLite/development: register a no-op IBackgroundJobClient
        builder.Services.AddSingleton<Hangfire.IBackgroundJobClient>(sp => 
            new FocusDeck.Server.Middleware.StubBackgroundJobClient());
    }

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AutomationDbContext>("database", tags: new[] { "db", "sql" })
        .AddCheck("filesystem", new FileSystemWriteHealthCheck(builder.Configuration), tags: new[] { "filesystem" });

    // Add telemetry throttle service
    builder.Services.AddSingleton<ITelemetryThrottleService, TelemetryThrottleService>();

    // Add CORS support with strict allow-list from configuration
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? new[] {
            "http://localhost:5173",          // Local dev (Vite default)
            "http://localhost:5000",          // Local dev
            "focusdeck-desktop://app",        // Desktop app
            "focusdeck-mobile://app"          // Mobile app
        };

    // Validate CORS configuration
    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException(
            "CORS configuration error: Cors:AllowedOrigins array cannot be empty. " +
            "Configure at least one allowed origin in appsettings.json.");
    }

    foreach (var origin in allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            throw new InvalidOperationException(
                "CORS configuration error: Allowed origins cannot contain null or empty values.");
        }

        // Validate that custom schemes or URIs are properly formatted
        if (!origin.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !origin.Contains("://"))
        {
            throw new InvalidOperationException(
                $"CORS configuration error: Invalid origin '{origin}'. " +
                "Origins must be absolute URIs (e.g., 'https://example.com' or 'app://scheme').");
        }
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("StrictCors", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE") // Explicit methods only
                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept")
                  .WithExposedHeaders("traceId") // Expose traceId for error tracking
                  .AllowCredentials(); // For cookies/auth tokens
        });
    });

    // Add JWT Authentication
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection.GetValue<string>("Key") ?? "super_dev_secret_key_change_me_please_32chars";
    var jwtIssuer = jwtSection.GetValue<string>("Issuer") ?? "https://focusdeck.909436.xyz";
    var jwtAudience = jwtSection.GetValue<string>("Audience") ?? "focusdeck-clients";

    // Validate JWT key configuration in production
    if (!builder.Environment.IsDevelopment())
    {
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT:Key must be configured with at least 32 characters in production. " +
                "Use environment variable JWT__Key or secure configuration provider.");
        }

        if (jwtKey.Contains("super_dev_secret") || jwtKey.Contains("change_me") || jwtKey.Contains("your-"))
        {
            throw new InvalidOperationException(
                "JWT:Key appears to be a placeholder. Configure a real secret key using environment variables or secure vault.");
        }
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[]
            {
                jwtIssuer,                      // https://focusdeck.909436.xyz
                "FocusDeckDev",                 // Legacy dev issuer
                "http://192.168.1.110:5000"     // Optional: allow old LAN tokens during transition
            },
            ValidateAudience = true,
            ValidAudiences = new[]
            {
                jwtAudience,                    // focusdeck-clients
                "FocusDeckClients",             // Legacy audience
                "local-dev"                     // Optional: local development
            },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
    builder.Services.AddAuthorization();

    // Add HTTP logging for debugging (optional but helpful)
    builder.Services.AddHttpLogging(_ => { });

    var app = builder.Build();

    // Add Serilog request logging with correlation IDs
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var correlationId = Activity.Current?.Id ?? httpContext.TraceIdentifier ?? "unknown";
            diagnosticContext.Set("CorrelationId", correlationId);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? "unknown");
        };
    });

    // Global exception handler
    app.UseMiddleware<GlobalExceptionHandler>();

    // CRITICAL: Configure forwarded headers for Cloudflare proxy
    // This makes ASP.NET Core treat requests as HTTPS and see the real client IP
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // Optional: Enable HTTP request/response logging during debugging
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpLogging();
    }

    // Initialize database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        db.Database.EnsureCreated();

        // Lightweight schema guard: add new columns if missing (SQLite)
        try
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(ConnectedServices);";
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    cols.Add(reader.GetString(1));
                }
            }
            var alterCmds = new List<string>();
            if (!cols.Contains("MetadataJson")) alterCmds.Add("ALTER TABLE ConnectedServices ADD COLUMN MetadataJson TEXT;");
            if (!cols.Contains("IsConfigured")) alterCmds.Add("ALTER TABLE ConnectedServices ADD COLUMN IsConfigured INTEGER NOT NULL DEFAULT 0;");
            foreach (var sql in alterCmds)
            {
                using var c2 = conn.CreateCommand();
                c2.CommandText = sql;
                await c2.ExecuteNonQueryAsync();
            }
        }
        catch { /* best-effort */ }

        // Ensure sync tables exist (SQLite) - best-effort CREATE IF NOT EXISTS
        try
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            var createCommands = new[]
            {
                // DeviceRegistrations
                @"CREATE TABLE IF NOT EXISTS DeviceRegistrations (
                    Id TEXT PRIMARY KEY,
                    DeviceId TEXT NOT NULL,
                    DeviceName TEXT NOT NULL,
                    Platform INTEGER NOT NULL,
                    UserId TEXT NOT NULL,
                    RegisteredAt TEXT NOT NULL,
                    LastSyncAt TEXT NOT NULL,
                    IsActive INTEGER NOT NULL,
                    AppVersion TEXT
                );",
                // SyncTransactions
                @"CREATE TABLE IF NOT EXISTS SyncTransactions (
                    Id TEXT PRIMARY KEY,
                    DeviceId TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    ErrorMessage TEXT
                );",
                // SyncChanges
                @"CREATE TABLE IF NOT EXISTS SyncChanges (
                    Id TEXT PRIMARY KEY,
                    TransactionId TEXT NOT NULL,
                    EntityType INTEGER NOT NULL,
                    EntityId TEXT NOT NULL,
                    Operation INTEGER NOT NULL,
                    DataJson TEXT NOT NULL,
                    ChangedAt TEXT NOT NULL,
                    ChangeVersion INTEGER NOT NULL
                );",
                // SyncMetadata
                @"CREATE TABLE IF NOT EXISTS SyncMetadata (
                    Id TEXT PRIMARY KEY,
                    DeviceId TEXT NOT NULL UNIQUE,
                    LastSyncVersion INTEGER NOT NULL,
                    LastSyncTime TEXT NOT NULL,
                    EntityVersions TEXT
                );",
                // SyncVersions (global version stamps)
                @"CREATE TABLE IF NOT EXISTS SyncVersions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CreatedAt TEXT NOT NULL
                );",
                // Notes
                @"CREATE TABLE IF NOT EXISTS Notes (
                    Id TEXT PRIMARY KEY,
                    Title TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Tags TEXT,
                    Color TEXT,
                    IsPinned INTEGER NOT NULL DEFAULT 0,
                    CreatedDate TEXT NOT NULL,
                    LastModified TEXT,
                    Bookmarks TEXT
                );",
                // StudySessions
                @"CREATE TABLE IF NOT EXISTS StudySessions (
                    SessionId TEXT PRIMARY KEY,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    DurationMinutes INTEGER NOT NULL,
                    SessionNotes TEXT,
                    Status INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FocusRate INTEGER,
                    BreaksCount INTEGER NOT NULL DEFAULT 0,
                    BreakDurationMinutes INTEGER NOT NULL DEFAULT 0,
                    Category TEXT
                );"
            };

            foreach (var sql in createCommands)
            {
                using var c = conn.CreateCommand();
                c.CommandText = sql;
                await c.ExecuteNonQueryAsync();
            }
        }
        catch { /* best-effort */ }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // SPA Fallback middleware - MUST RUN BEFORE StaticFiles so rewrites are served
    // For directory requests or routes without extensions, serve index.html to enable client-side routing
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/app"))
        {
            var path = context.Request.Path.Value ?? "/app";
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            
            // Check if this is a real file that should exist
            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                path.TrimStart('/'));
            
            logger.LogDebug("SPA Fallback: Checking path={Path}, filePath={FilePath}", path, filePath);
            
            // If path has an extension (like .css, .js, .svg), don't rewrite
            if (Path.HasExtension(path))
            {
                logger.LogDebug("SPA Fallback: Path has extension, passing through");
                await next();
                return;
            }
            
            // If it's a directory, append index.html
            if (Directory.Exists(filePath))
            {
                filePath = Path.Combine(filePath, "index.html");
                logger.LogDebug("SPA Fallback: Is directory, checking for index.html at {FilePath}", filePath);
            }
            
            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                logger.LogDebug("SPA Fallback: File exists at {FilePath}, serving index.html", filePath);
                context.Request.Path = "/app/index.html";
            }
            else
            {
                logger.LogDebug("SPA Fallback: File not found at {FilePath}, also 404", filePath);
            }
        }
        await next();
    });

    // Serve static files (Web UI)
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // For non-HTML files, use aggressive caching.
            // HTML files are served by the custom endpoint below and will have their own headers.
            if (!ctx.File.Name.EndsWith(".html"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800"); // 7 days
            }
        }
    });

    // Serve static files from wwwroot/app (Vite build output)
    var appAssetsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "app");
    if (Directory.Exists(appAssetsPath))
    {
        var provider = new FileExtensionContentTypeProvider();
        // Ensure CSS and JS have correct MIME types
        provider.Mappings[".css"] = "text/css";
        provider.Mappings[".js"] = "application/javascript";
        provider.Mappings[".mjs"] = "application/javascript";
        provider.Mappings[".json"] = "application/json";
        provider.Mappings[".svg"] = "image/svg+xml";
        
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(appAssetsPath),
            RequestPath = "/app",
            ContentTypeProvider = provider,
            OnPrepareResponse = ctx =>
            {
                // Cache static assets (JS, CSS, images) - use immutable cache for content-hashed files
                if (!ctx.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=31536000,immutable";
                }
            }
        });
    }

    // Enable CORS with strict policy
    app.UseCors("StrictCors");

    // Enable Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FocusDeck API v1");
        options.RoutePrefix = "swagger"; // Access at /swagger
        options.DocumentTitle = "FocusDeck API Documentation";
        options.DisplayRequestDuration();
    });

    // Only redirect to HTTPS in development (not behind Cloudflare proxy)
    // Cloudflare handles HTTPS termination, sends HTTP to our server
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // AuthN/Z
    app.UseAuthentication();
    app.UseAuthorization();

    // Hangfire Dashboard (protected by authorization) - only if using PostgreSQL
    if (hangfireConnection.Contains("Host=") || hangfireConnection.Contains("Server="))
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DashboardTitle = "FocusDeck Background Jobs"
        });
    }

    // Map API controllers with authorization
    app.MapControllers()
        .RequireAuthorization(); // All API controllers require auth by default

    // Map SignalR hub
    app.MapHub<NotificationsHub>("/hubs/notifications")
        .RequireAuthorization();

    // Health check endpoint (no auth required)
    app.MapGet("/healthz", () => Results.Ok(new { ok = true, time = DateTimeOffset.UtcNow }))
        .WithName("HealthCheck")
        .WithOpenApi()
        .AllowAnonymous();

    // System health check endpoint with detailed checks (no auth required)
    app.MapHealthChecks("/v1/system/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
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
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            };
            await context.Response.WriteAsJsonAsync(response);
        }
    }).AllowAnonymous();

    // Add security headers for SPA
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/app"))
        {
            // Content Security Policy
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'none';";
            
            // Other security headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            // Cache control for HTML files
            if (context.Request.Path.Value?.EndsWith("index.html") == true)
            {
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }
        }
        
        await next();
    });

    // Custom endpoint to serve the root index.html with version injection
    app.MapGet("/", async (HttpContext context, VersionService versionService, IWebHostEnvironment env) =>
    {
        var version = versionService.GetVersion();
        
        var indexPath = Path.Combine(env.WebRootPath, "index.html");
        if (!File.Exists(indexPath))
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("index.html not found");
            return;
        }
        
        var indexHtml = await File.ReadAllTextAsync(indexPath);
        
        // Replace all instances of the placeholder with the actual version
        indexHtml = indexHtml.Replace("__VERSION__", version);
        
        context.Response.ContentType = "text/html";
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        
        await context.Response.WriteAsync(indexHtml);
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to integration tests
public partial class Program { }
