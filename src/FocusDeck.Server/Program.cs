using FocusDeck.Server.Services;
using FocusDeck.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add HttpClient for OAuth token exchange
builder.Services.AddHttpClient();

// Add Database
builder.Services.AddDbContext<AutomationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=focusdeck.db"));

// Add Sync Service
builder.Services.AddScoped<ISyncService, SyncService>();

// Add Action Executor
builder.Services.AddSingleton<ActionExecutor>();

// Add background services
builder.Services.AddHostedService<AutomationEngine>();

// Add Version Service
builder.Services.AddSingleton<VersionService>();

// Add CORS support for web UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? "super_dev_secret_key_change_me_please_32chars";
var jwtIssuer = jwtSection.GetValue<string>("Issuer") ?? "FocusDeckDev";
var jwtAudience = jwtSection.GetValue<string>("Audience") ?? "FocusDeckClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

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

// Enable CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// AuthN/Z
app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();

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

