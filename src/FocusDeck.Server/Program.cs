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
using Microsoft.AspNetCore.Http;
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
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Memory;
using FocusDeck.Server.Services.Tenancy;
using FocusDeck.Server.Services.Auditing;
using FocusDeck.SharedKernel.Tenancy;
using FocusDeck.SharedKernel.Auditing;

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
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IAuditActorProvider, HttpContextAuditActorProvider>();

    // Rate limiting for auth endpoints (per-IP)
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("AuthBurst", context =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });
    });

    // Add Database (SQLite as default, PostgreSQL connection string can be configured)
    var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "focusdeck.db");
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? $"Data Source={dbPath}";
    
    // Ensure the directory for the SQLite database exists
    var dbDirectory = Path.GetDirectoryName(dbPath);
    if (dbDirectory != null && !Directory.Exists(dbDirectory))
    {
        Directory.CreateDirectory(dbDirectory);
    }
    
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
    builder.Services.AddSingleton<FocusDeck.Services.Abstractions.IEncryptionService, FocusDeck.Services.Implementations.Core.EncryptionService>();

    // Redis cache for revocation/pub-sub (optional)
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
    }

    // Add Auth services
    builder.Services.AddScoped<FocusDeck.Server.Services.Auth.ITokenService, FocusDeck.Server.Services.Auth.TokenService>();
    builder.Services.AddSingleton<FocusDeck.Server.Services.Auth.ISrpSessionCache, FocusDeck.Server.Services.Auth.SrpSessionCache>();
    builder.Services.AddSingleton<FocusDeck.Server.Services.Auth.IAuthAttemptLimiter>(sp =>
    {
        var redis = sp.GetService<IConnectionMultiplexer>();
        var memoryCache = sp.GetRequiredService<IMemoryCache>();
        return new FocusDeck.Server.Services.Auth.AuthAttemptLimiter(redis, memoryCache);
    });
    builder.Services.AddScoped<ITenantMembershipService, TenantMembershipService>();
    builder.Services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();

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

    // Platform activity detection + context aggregation
    builder.Services.AddSingleton<FocusDeck.Services.Activity.IActivityDetectionService, FocusDeck.Server.Services.Activity.LinuxActivityDetectionService>();
    builder.Services.AddSingleton<FocusDeck.Server.Services.Integrations.CanvasService>();
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<FocusDeck.Server.Services.Integrations.ICanvasCache, FocusDeck.Server.Services.Integrations.CanvasCache>();
    builder.Services.AddHostedService<FocusDeck.Server.Services.Integrations.CanvasSyncService>();
    builder.Services.AddSingleton<FocusDeck.Server.Services.Auth.IUserConnectionTracker, FocusDeck.Server.Services.Auth.UserConnectionTracker>();
    builder.Services.AddSingleton<FocusDeck.Server.Services.Context.IContextAggregationService, FocusDeck.Server.Services.Context.ContextAggregationService>();
    builder.Services.AddHostedService<FocusDeck.Server.Services.Context.ContextBroadcastService>();

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
    var healthChecks = builder.Services.AddHealthChecks()
        .AddDbContextCheck<AutomationDbContext>("database", tags: new[] { "db", "sql" })
        .AddCheck("filesystem", new FileSystemWriteHealthCheck(builder.Configuration), tags: new[] { "filesystem" });

    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        healthChecks.AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "redis" });
    }

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

        // Revocation check via OnTokenValidated
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                try
                {
                    var revocation = ctx.HttpContext.RequestServices.GetRequiredService<FocusDeck.Server.Services.Auth.IAccessTokenRevocationService>();
                    var jti = ctx.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti))
                    {
                        if (await revocation.IsRevokedAsync(jti, ctx.HttpContext.RequestAborted))
                        {
                            ctx.Fail("Token revoked");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Revocation check failed");
                }
            }
        };
    });
    builder.Services.AddAuthorization();
    builder.Services.AddHostedService<FocusDeck.Server.Services.Auth.TokenPruningService>();

    // Revocation service
    builder.Services.AddScoped<FocusDeck.Server.Services.Auth.IAccessTokenRevocationService, FocusDeck.Server.Services.Auth.AccessTokenRevocationService>();

    // Add HTTP logging for debugging (optional but helpful)
    builder.Services.AddHttpLogging(_ => { });

    var app = builder.Build();

    static bool ShouldServeSpa(HttpContext context, bool includeRoot)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isRoot = string.IsNullOrEmpty(path) || string.Equals(path, "/", StringComparison.OrdinalIgnoreCase);

        if (context.Request.Path.StartsWithSegments("/v1") ||
            context.Request.Path.StartsWithSegments("/hubs") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/healthz") ||
            context.Request.Path.StartsWithSegments("/hangfire"))
        {
            return false;
        }

        if (path.StartsWith("/app", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (isRoot)
        {
            return includeRoot;
        }

        return !Path.HasExtension(path);
    }

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

    // Enable rate limiting
    app.UseRateLimiter();

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
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migration failed; proceeding with best-effort initialization");
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            if (!ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
            }
        }
    });

    app.MapWhen(
        ctx => ShouldServeSpa(ctx, includeRoot: false),
        spa => spa.Run(async context =>
        {
            var indexFile = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
            if (!System.IO.File.Exists(indexFile))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("SPA assets not found. Run 'npm run build' in src/FocusDeck.WebApp.'");
                return;
            }

            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(indexFile);
        }));

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
        if (ShouldServeSpa(context, includeRoot: true))
        {
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'none';";

            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            if (string.Equals(context.Request.Path.Value, "/", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.Value?.EndsWith("index.html", StringComparison.OrdinalIgnoreCase) == true)
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

    // Redirect any legacy /app paths to the root SPA
    app.MapFallback("/app/{*path}", (HttpContext context, string? path) =>
    {
        var normalized = (path ?? string.Empty).Trim('/');
        var redirectTarget = string.IsNullOrEmpty(normalized) ? "/" : $"/{normalized}";
        var query = context.Request.QueryString.Value ?? string.Empty;
        return Results.Redirect(string.Concat(redirectTarget, query), permanent: true);
    }).AllowAnonymous();

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
