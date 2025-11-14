using System.Diagnostics;
using System.Text;
using System.Threading.RateLimiting;
using FocusDeck.Persistence;
using FocusDeck.Server.HealthChecks;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Jobs;
using FocusDeck.Server.Middleware;
using FocusDeck.Server.Services;
using FocusDeck.Server.Services.Auditing;
using FocusDeck.Server.Services.Auth;
using FocusDeck.Server.Services.Tenancy;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.SharedKernel;
using System.Linq;
using System.Text.Json;
using FocusDeck.SharedKernel.Auditing;
using FocusDeck.SharedKernel.Tenancy;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;

namespace FocusDeck.Server;

public sealed class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // OpenTelemetry (tracing only, as in Program)
        services.AddOpenTelemetry()
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

        // MVC controllers
        services.AddControllers();

        // Swagger / OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
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

        // API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // HttpClient and HttpContext access
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditActorProvider, HttpContextAuditActorProvider>();

        // Rate limiting for auth endpoints (per-IP)
        services.AddRateLimiter(options =>
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

        // Database (SQLite default, optional PostgreSQL)
        var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "focusdeck.db");
        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? $"Data Source={dbPath}";

        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (dbDirectory != null && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        if (connectionString.Contains("Host=") || connectionString.Contains("Server="))
        {
            services.AddDbContext<AutomationDbContext>(options => options.UseNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<AutomationDbContext>(options => options.UseSqlite(connectionString));
        }

        // SharedKernel services
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.AddSingleton<FocusDeck.Services.Abstractions.IEncryptionService, FocusDeck.Services.Implementations.Core.EncryptionService>();

        // Redis (optional)
        var redisConnection = _configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        // Auth services
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<ISrpSessionCache, SrpSessionCache>();
        services.AddMemoryCache();
        services.AddSingleton<IAuthAttemptLimiter>(sp =>
        {
            var redis = sp.GetService<IConnectionMultiplexer>();
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            return new AuthAttemptLimiter(redis, memoryCache);
        });

        // Tenancy
        services.AddScoped<ITenantMembershipService, TenantMembershipService>();
        services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();

        // Storage
        services.AddSingleton<FocusDeck.Server.Services.Storage.IAssetStorage, FocusDeck.Server.Services.Storage.LocalFileSystemAssetStorage>();

        // Transcription & TextGen
        services.AddSingleton<FocusDeck.Server.Services.Transcription.IWhisperAdapter, FocusDeck.Server.Services.Transcription.StubWhisperAdapter>();
        services.AddSingleton<FocusDeck.Server.Services.TextGeneration.ITextGen, FocusDeck.Server.Services.TextGeneration.StubTextGen>();

        // Jobs
        services.AddScoped<ITranscribeLectureJob, TranscribeLectureJob>();
        services.AddScoped<ISummarizeLectureJob, SummarizeLectureJob>();
        services.AddScoped<IVerifyNoteJob, VerifyNoteJob>();
        services.AddScoped<IGenerateLectureNoteJob, GenerateLectureNoteJob>();

        // Sync & automation
        services.AddScoped<ISyncService, SyncService>();
        services.AddSingleton<ActionExecutor>();
        services.AddSingleton<IServerUpdateService, ServerUpdateService>();
        services.AddHostedService<AutomationEngine>();

        // Version service
        services.AddSingleton<VersionService>();

        // SignalR
        services.AddSignalR();

        // Activity + context aggregation
        services.AddSingleton<FocusDeck.Services.Activity.IActivityDetectionService, FocusDeck.Server.Services.Activity.LinuxActivityDetectionService>();
        services.AddSingleton<FocusDeck.Server.Services.Integrations.CanvasService>();
        services.AddSingleton<FocusDeck.Server.Services.Integrations.ICanvasCache, FocusDeck.Server.Services.Integrations.CanvasCache>();
        services.AddHostedService<FocusDeck.Server.Services.Integrations.CanvasSyncService>();
        services.AddSingleton<FocusDeck.Server.Services.Auth.IUserConnectionTracker, FocusDeck.Server.Services.Auth.UserConnectionTracker>();
        services.AddSingleton<FocusDeck.Server.Services.Context.IContextAggregationService, FocusDeck.Server.Services.Context.ContextAggregationService>();
        services.AddHostedService<FocusDeck.Server.Services.Context.ContextBroadcastService>();

        // Jarvis workflow registry
        services.AddSingleton<IJarvisWorkflowRegistry, JarvisWorkflowRegistry>();

        // Hangfire
        var hangfireConnection = _configuration.GetConnectionString("HangfireConnection")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=focusdeck.db";

        if (hangfireConnection.Contains("Host=") || hangfireConnection.Contains("Server="))
        {
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection)));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 5;
                options.ServerName = $"FocusDeck-{Environment.MachineName}";
            });
        }
        else
        {
            services.AddSingleton<Hangfire.IBackgroundJobClient>(_ => new StubBackgroundJobClient());
        }

        // Health checks
        var healthChecks = services.AddHealthChecks()
            .AddDbContextCheck<AutomationDbContext>("database", tags: new[] { "db", "sql" })
            .AddCheck("filesystem", new FileSystemWriteHealthCheck(_configuration), tags: new[] { "filesystem" });

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            healthChecks.AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "redis" });
        }

        // Telemetry throttle
        services.AddSingleton<ITelemetryThrottleService, TelemetryThrottleService>();

        // CORS
        var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[]
            {
                "http://localhost:5173",
                "http://localhost:5000",
                "focusdeck-desktop://app",
                "focusdeck-mobile://app"
            };

        if (allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("CORS configuration error: Cors:AllowedOrigins array cannot be empty.");
        }

        foreach (var origin in allowedOrigins)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                throw new InvalidOperationException("CORS configuration error: Allowed origins cannot contain null or empty values.");
            }

            if (!origin.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !origin.Contains("://"))
            {
                throw new InvalidOperationException($"CORS configuration error: Invalid origin '{origin}'.");
            }
        }

        services.AddCors(options =>
        {
            options.AddPolicy("StrictCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                      .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept")
                      .WithExposedHeaders("traceId")
                      .AllowCredentials();
            });
        });

        // JWT configuration
        var jwtSection = _configuration.GetSection("Jwt");
        var jwtKey = jwtSection.GetValue<string>("Key") ?? "super_dev_secret_key_change_me_please_32chars";
        var jwtIssuer = jwtSection.GetValue<string>("Issuer") ?? "https://focusdeck.909436.xyz";
        var jwtAudience = jwtSection.GetValue<string>("Audience") ?? "focusdeck-clients";

        if (!_environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                throw new InvalidOperationException("JWT:Key must be configured with at least 32 characters in production.");
            }

            if (jwtKey.Contains("super_dev_secret") || jwtKey.Contains("change_me") || jwtKey.Contains("your-"))
            {
                throw new InvalidOperationException("JWT:Key appears to be a placeholder. Configure a real secret key.");
            }
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[]
            {
                jwtIssuer,
                "FocusDeckDev",
                "http://192.168.1.110:5000"
            },
            ValidateAudience = true,
            ValidAudiences = new[]
            {
                jwtAudience,
                "FocusDeckClients",
                "local-dev"
            },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        services.AddSingleton(tokenValidationParameters);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = tokenValidationParameters;
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async ctx =>
                {
                    try
                    {
                        var revocation = ctx.HttpContext.RequestServices.GetRequiredService<IAccessTokenRevocationService>();
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
                },
                OnAuthenticationFailed = ctx =>
                {
                    var reason = ctx.Exception switch
                    {
                        SecurityTokenExpiredException => "expired",
                        SecurityTokenException => "invalid",
                        _ => "invalid"
                    };
                    AuthTelemetry.RecordJwtValidationFailure(reason);
                    Log.Warning(ctx.Exception, "JWT authentication failed ({Reason})", reason);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        services.AddHostedService<TokenPruningService>();
        services.AddScoped<IAccessTokenRevocationService, AccessTokenRevocationService>();

        // HTTP logging
        services.AddHttpLogging(_ => { });
    }

    public void Configure(IApplicationBuilder app)
    {
        var env = _environment;
        var services = app.ApplicationServices;

        // Serilog request logging
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

        // Rate limiting
        app.UseRateLimiter();

        // Forwarded headers
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // HTTP logging in development
        if (env.IsDevelopment())
        {
            app.UseHttpLogging();
        }

        // Initialize database (best-effort)
        using (var scope = services.CreateScope())
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

        // Backfill PAKE credential salts from KDF metadata where the SaltBase64 column is empty.
        // This helps migrations for older credentials that had KDF JSON but no explicit salt column value.
        using (var seedScope = services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            try
            {
                var rows = db.PakeCredentials
                    .Where(p => string.IsNullOrWhiteSpace(p.SaltBase64) && !string.IsNullOrWhiteSpace(p.KdfParametersJson))
                    .ToListAsync()
                    .GetAwaiter()
                    .GetResult();

                if (rows.Count > 0)
                {
                    Log.Information("Backfilling {Count} PAKE credential(s) with salt from KDF metadata", rows.Count);
                    foreach (var row in rows)
                    {
                        try
                        {
                            var kdf = JsonSerializer.Deserialize<FocusDeck.Shared.Security.SrpKdfParameters>(row.KdfParametersJson!);
                            if (kdf != null && !string.IsNullOrWhiteSpace(kdf.SaltBase64))
                            {
                                row.SaltBase64 = kdf.SaltBase64;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Failed to parse KDF parameters for user {UserId}", row.UserId);
                        }
                    }

                    db.SaveChangesAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to backfill PAKE salts from KDF metadata");
            }
        }

        var webApp = app as WebApplication;

        if (env.IsDevelopment() && webApp != null)
        {
            webApp.MapOpenApi();
        }

        app.UseDefaultFiles();

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
                else
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
                }
            }
        });

        if (webApp != null)
        {
            webApp.MapWhen(
                ctx => ShouldServeSpa(ctx, includeRoot: false),
                spa => spa.Run(async context =>
                {
                    var indexFile = Path.Combine(webApp.Environment.WebRootPath ?? "wwwroot", "index.html");
                    if (!File.Exists(indexFile))
                    {
                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        await context.Response.WriteAsync("SPA assets not found. Run 'npm run build' in src/FocusDeck.WebApp.'");
                        return;
                    }

                    context.Response.ContentType = "text/html";
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";
                    await context.Response.SendFileAsync(indexFile);
                }));
        }

        app.UseCors("StrictCors");

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FocusDeck API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "FocusDeck API Documentation";
            options.DisplayRequestDuration();
        });

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAuthenticationMiddleware();

        var hangfireConnection = _configuration.GetConnectionString("HangfireConnection")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=focusdeck.db";

        if (hangfireConnection.Contains("Host=") || hangfireConnection.Contains("Server="))
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
                DashboardTitle = "FocusDeck Background Jobs"
            });
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();

            endpoints.MapHub<NotificationsHub>("/hubs/notifications")
                .RequireAuthorization();

            endpoints.MapGet("/healthz", () => Results.Ok(new { ok = true, time = DateTimeOffset.UtcNow }))
                .WithName("HealthCheck")
                .WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());

            endpoints.MapHealthChecks("/v1/system/health", new HealthCheckOptions
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
            }).WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());

            endpoints.MapGet("/", async (HttpContext context, VersionService versionService, IWebHostEnvironment env2) =>
            {
                var version = versionService.GetVersion();

                var indexPath = Path.Combine(env2.WebRootPath ?? "wwwroot", "index.html");
                if (!File.Exists(indexPath))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("index.html not found");
                    return;
                }

                var indexHtml = await File.ReadAllTextAsync(indexPath);
                indexHtml = indexHtml.Replace("__VERSION__", version);

                context.Response.ContentType = "text/html";
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";

                await context.Response.WriteAsync(indexHtml);
            });

            endpoints.MapFallback("/app/{*path}", (HttpContext context, string? path) =>
            {
                var normalized = (path ?? string.Empty).Trim('/');
                var redirectTarget = string.IsNullOrEmpty(normalized) ? "/" : $"/{normalized}";
                var query = context.Request.QueryString.Value ?? string.Empty;
                return Results.Redirect(string.Concat(redirectTarget, query), permanent: true);
            }).WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());
        });

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
    }

    private static bool ShouldServeSpa(HttpContext context, bool includeRoot)
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
}
