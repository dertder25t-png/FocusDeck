using FocusDeck.Server.Services;
using FocusDeck.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add Database
builder.Services.AddDbContext<AutomationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=focusdeck.db"));

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

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
    db.Database.EnsureCreated();
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

