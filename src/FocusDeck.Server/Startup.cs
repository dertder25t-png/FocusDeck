using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.RateLimiting;
using FocusDeck.Persistence;
using FocusDeck.Server.HealthChecks;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Jobs;
using FocusDeck.Server.Middleware;
using FocusDeck.Server.Services;
using FocusDeck.Server.Services.Automations;
using FocusDeck.Server.Services.Calendar;
using FocusDeck.Server.Services.Context;
using FocusDeck.Server.Services.Auditing;
using FocusDeck.Server.Services.Auth;
using FocusDeck.Server.Configuration;
using FocusDeck.Server.Services.Burnout;
using FocusDeck.Server.Services.Privacy;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.Server.Jobs.Jarvis;
using FocusDeck.Server.Services.Tenancy;
using FocusDeck.Server.Services.Security;
using FocusDeck.Services.Jarvis;
using FocusDeck.Persistence.Repositories;
using FocusDeck.Persistence.Repositories.Context;
using FocusDeck.Persistence.Repositories.Jarvis;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Services.Context;
using FocusDeck.Services.Context.Sources;
using FocusDeck.SharedKernel;
using System.Linq;
using System.Text.Json;
using FocusDeck.SharedKernel.Auditing;
using FocusDeck.SharedKernel.Tenancy;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
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
        // OpenTelemetry (Tracing + Metrics)
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
                    .AddConsoleExporter()
                    .AddOtlpExporter();
            })
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FocusDeck.Server"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
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

        var databaseProvider = _configuration["DatabaseProvider"];
        if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Host=") ||
            connectionString.Contains("Server="))
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

        // Data Protection for API key encryption
        var dataProtectionPath = Path.Combine(AppContext.BaseDirectory, "data", "keys");
        if (!Directory.Exists(dataProtectionPath))
        {
            Directory.CreateDirectory(dataProtectionPath);
        }
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
            .SetApplicationName("FocusDeck");
        services.AddScoped<IApiKeyEncryptionService, ApiKeyEncryptionService>();

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
        services.AddTransient<BackgroundWorkerTenant>(); // For background jobs explicitly requesting it

        // Storage
        services.AddSingleton<FocusDeck.Server.Services.Storage.IAssetStorage, FocusDeck.Server.Services.Storage.LocalFileSystemAssetStorage>();

        // Transcription & TextGen
        services.AddSingleton<FocusDeck.Server.Services.Transcription.IWhisperAdapter, FocusDeck.Server.Services.Transcription.StubWhisperAdapter>();
        services.AddHttpClient<FocusDeck.Server.Services.TextGeneration.ITextGen, FocusDeck.Server.Services.TextGeneration.GeminiTextGenService>();
        services.AddScoped<FocusDeck.Contracts.Services.Context.IEmbeddingGenerationService, FocusDeck.Server.Services.Context.GeminiEmbeddingService>();

        // Automation
        services.AddSingleton<IYamlAutomationLoader, YamlAutomationLoader>();
        services.AddSingleton<ActionExecutor>();
        services.AddHostedService<AutomationEngine>();

        // Jobs
        services.AddScoped<VectorizePendingSnapshotsJob>();
        services.AddScoped<CalendarWarmSyncJob>();

        // Version service
        services.AddSingleton<VersionService>();

        // SignalR
        services.AddSignalR();

        // Activity + context aggregation
        services.AddScoped<CalendarResolver>();
        services.AddSingleton<FocusDeck.Services.Activity.IActivityDetectionService, FocusDeck.Server.Services.Activity.LinuxActivityDetectionService>();
        services.AddSingleton<FocusDeck.Server.Services.Integrations.CanvasService>();
        services.AddSingleton<FocusDeck.Server.Services.Integrations.ICanvasCache, FocusDeck.Server.Services.Integrations.CanvasCache>();
        services.AddHostedService<FocusDeck.Server.Services.Integrations.CanvasSyncService>();
        services.AddSingleton<FocusDeck.Server.Services.Auth.IUserConnectionTracker, FocusDeck.Server.Services.Auth.UserConnectionTracker>();
        services.AddSingleton<FocusDeck.Server.Services.Context.IContextAggregationService, FocusDeck.Server.Services.Context.ContextAggregationService>();
        services.AddHostedService<FocusDeck.Server.Services.Context.ContextBroadcastService>();
        services.AddScoped<IPrivacyService, PrivacyService>();
        services.AddScoped<FocusDeck.Contracts.Services.Privacy.IPrivacyDataNotifier, SignalRPrivacyDataNotifier>();
        services.AddScoped<IBurnoutAnalysisService, BurnoutAnalysisService>();
        services.AddScoped<FocusDeck.Server.Services.Context.ISnapshotIngestService, FocusDeck.Server.Services.Context.SnapshotIngestService>();
        services.AddScoped<IEfContextSnapshotRepository, EfContextSnapshotRepository>();
        services.AddScoped<IContextSnapshotRepository, EfContextSnapshotRepository>();
        services.AddScoped<IActivitySignalRepository, FocusDeck.Persistence.Repositories.EfActivitySignalRepository>();
        services.AddSingleton<IVectorStore, VectorStoreStub>();
        services.AddScoped<IEventCacheRepository, EfEventCacheRepository>();
        services.AddScoped<AmbientService>();
        services.AddScoped<KnowledgeVaultService>();
        // Context snapshot infrastructure
        services.AddSingleton<FocusDeck.Server.Services.Context.IContextEventBus, FocusDeck.Server.Services.Context.ContextEventBus>();
        services.AddScoped<FocusDeck.Contracts.Services.Context.IContextRetrievalService, FocusDeck.Server.Services.Context.ContextRetrievalService>();
        services.AddScoped<FocusDeck.Server.Services.Browser.IBrowserContextService, FocusDeck.Server.Services.Browser.BrowserContextService>();
        services.AddScoped<IContextSnapshotService, ContextSnapshotService>();
        services.AddScoped<IWorkspaceSnapshotService, WorkspaceSnapshotService>();
        services.AddScoped<IContextSnapshotSource, DesktopActiveWindowSource>();
        services.AddScoped<IContextSnapshotSource, GoogleCalendarSource>();
        services.AddScoped<IContextSnapshotSource, CanvasAssignmentsSource>();
        services.AddScoped<IContextSnapshotSource, SpotifySource>();
        services.AddScoped<IContextSnapshotSource, DeviceActivitySource>();
        services.AddScoped<IContextSnapshotSource, SuggestiveContextSource>();

        // Jarvis workflow registry
        services.AddScoped<IJarvisWorkflowRegistry, JarvisWorkflowRegistry>();
        services.AddScoped<FocusDeck.Server.Services.Jarvis.IAutomationGeneratorService, FocusDeck.Server.Services.Jarvis.AutomationGeneratorService>();
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddHostedService<ImplicitFeedbackMonitor>();
        services.AddScoped<ILayeredContextService, LayeredContextService>();
        services.AddScoped<IExampleGenerator, ExampleGenerator>();
        services.AddScoped<IProjectSortingService, ProjectSortingService>();
        services.AddScoped<FocusDeck.Server.Services.Writing.ICitationEngine, FocusDeck.Server.Services.Writing.CitationEngine>();
        services.AddScoped<IJarvisRunRepository, EfJarvisRunRepository>();
        services.AddScoped<IJarvisRunService, JarvisRunService>();
        services.AddScoped<IJarvisActionDispatcher, JarvisActionDispatcher>();
        services.AddScoped<IJarvisActionHandler, NoOpActionHandler>();
        services.AddScoped<IJarvisRunJob, JarvisRunJob>();
        services.AddScoped<FocusDeck.Server.Jobs.Jarvis.PatternRecognitionJob>();

        // Behavioral Clustering
        services.AddScoped<FocusDeck.Server.Services.Jarvis.Clustering.IBehavioralClusteringService, FocusDeck.Server.Services.Jarvis.Clustering.RuleBasedClusteringService>();
        services.AddScoped<FocusDeck.Server.Services.Jarvis.PatternRecognitionJob>();

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
            .AddCheck("filesystem", new FileSystemWriteHealthCheck(_configuration), tags: new[] { "filesystem" })
            .AddCheck<JwtKeyHealthCheck>("jwt_keys", tags: new[] { "security", "jwt" });

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
        var jwtSection = _configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
        jwtSettings.Validate();
        services.Configure<JwtSettings>(jwtSection);
        services.AddSingleton(jwtSettings);

        // Google Auth
        services.Configure<GoogleOptions>(_configuration.GetSection(GoogleOptions.SectionName));
        services.AddScoped<GoogleAuthService>();

        var vaultUrl = _configuration["Azure:KeyVault:VaultUrl"];
        if (!string.IsNullOrWhiteSpace(vaultUrl))
        {
            services.AddSingleton(sp => new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential()));
            services.AddSingleton<ICryptographicKeyStore, AzureKeyVaultKeyStore>();
        }
        else
        {
            if (_environment.IsProduction())
            {
                // In production, we log a warning but allow environment variable fallback if Azure is not configured
                Console.WriteLine("WARNING: Azure Key Vault is not configured in Production. Falling back to EnvironmentVariableKeyStore.");
            }
            services.AddSingleton<EnvironmentVariableKeyStore>(sp => new EnvironmentVariableKeyStore(jwtSettings));
            services.AddSingleton<ICryptographicKeyStore>(sp => sp.GetRequiredService<EnvironmentVariableKeyStore>());
        }

        services.AddSingleton<IJwtSigningKeyProvider>(sp => new JwtSigningKeyProvider(
            sp.GetRequiredService<ICryptographicKeyStore>(),
            sp.GetRequiredService<IOptions<JwtSettings>>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<ILogger<JwtSigningKeyProvider>>()));

        services.AddSingleton<TokenValidationParameters>(sp =>
        {
            var provider = sp.GetRequiredService<IJwtSigningKeyProvider>();
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = jwtSettings.GetValidIssuers(),
                ValidateAudience = true,
                ValidAudiences = jwtSettings.GetValidAudiences(),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                // NOTE: IssuerSigningKeys will be set dynamically by JwtBearerOptionsConfigurator via IssuerSigningKeyResolver
                // Do NOT call provider.GetValidationKeys() here as it may not have been properly initialized yet
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    // This will be overridden by JwtBearerOptionsConfigurator
                    return provider.GetValidationKeys();
                },
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });

        services.AddSingleton<IConfigureNamedOptions<JwtBearerOptions>, JwtBearerOptionsConfigurator>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddAuthorization();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHostedService<TokenPruningService>();
        services.AddScoped<IAccessTokenRevocationService, AccessTokenRevocationService>();

        if (!_environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<JwtKeyRotationService>();
        }

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

        // Prometheus metrics
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

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
        app.UseMiddleware<TenancyMiddleware>();
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
            RecurringJob.AddOrUpdate<BurnoutCheckJob>(
                "burnout-check-job",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 */2 * * *");

            RecurringJob.AddOrUpdate<VectorizePendingSnapshotsJob>(
                "vectorize-pending-snapshots",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/1 * * * *"); // Run every minute

            // Note: Use the new service-based PatternRecognitionJob, not the old job namespace if it existed.
            // The file created was in FocusDeck.Server.Services.Jarvis namespace.
            RecurringJob.AddOrUpdate<FocusDeck.Server.Services.Jarvis.PatternRecognitionJob>(
                "pattern-recognition-job",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 * * * *"); // Run every hour

            RecurringJob.AddOrUpdate<CalendarWarmSyncJob>(
                "calendar-warm-sync",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/30 * * * *"); // Run every 30 minutes

            RecurringJob.AddOrUpdate<SummarizeCapturedContentJob>(
                "summarize-captured-content",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/15 * * * *"); // Run every 15 minutes
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();

            endpoints.MapHub<NotificationsHub>("/hubs/notifications")
                .RequireAuthorization();

            endpoints.MapHub<PrivacyDataHub>("/hubs/privacydata")
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
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://static.cloudflareinsights.com; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self' data: https:; " +
                    "connect-src 'self' ws: wss: https://cloudflareinsights.com; " +
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
