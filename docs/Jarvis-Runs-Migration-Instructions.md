# Jarvis Runs Migration Instructions

To add the `DbSet` properties for the Jarvis Runs to the `AutomationDbContext`, add the following code to `src/FocusDeck.Persistence/AutomationDbContext.cs`:

```csharp
    // Jarvis Runs
    public DbSet<JarvisRun> JarvisRuns { get; set; }
    public DbSet<JarvisRunStep> JarvisRunSteps { get; set; }
```

To create and apply the migration for the Jarvis Runs system, run the following commands from the root of the repository:

```bash
sudo docker run --rm -v $(pwd):/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 dotnet ef migrations add AddJarvisRuns -p src/FocusDeck.Persistence -s src/FocusDeck.Server
sudo docker run --rm -v $(pwd):/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 dotnet ef database update -p src/FocusDeck.Persistence -s src/FocusDeck.Server
```
