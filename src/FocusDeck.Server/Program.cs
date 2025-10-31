var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Serve static files (Web UI)
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Map API controllers
app.MapControllers();

// Map API controllers
app.MapControllers();

app.Run();

