# Context Snapshot Migration Instructions

To create and apply the migration for the context snapshot system, run the following commands from the root of the repository:

```bash
sudo docker run --rm -v $(pwd):/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 dotnet ef migrations add AddContextSnapshots -p src/FocusDeck.Persistence -s src/FocusDeck.Server
sudo docker run --rm -v $(pwd):/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 dotnet ef database update -p src/FocusDeck.Persistence -s src/FocusDeck.Server
```
