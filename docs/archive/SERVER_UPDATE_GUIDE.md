# How to Update Your Running FocusDeck Server

Your changes have been pushed to GitHub! Here's how to update your running server.

## Quick Update (Windows - Development)

If you're running the server locally for testing:

### Option 1: Simple Rebuild and Restart (Fastest)

```powershell
# Stop the running server (Ctrl+C in the terminal where it's running)

# Navigate to the server directory
cd c:\Users\Caleb\Desktop\FocusDeck\src\FocusDeck.Server

# Pull the latest changes from GitHub
git pull origin master

# Go back to root
cd c:\Users\Caleb\Desktop\FocusDeck

# Rebuild the project
dotnet build .\src\FocusDeck.Server\FocusDeck.Server.csproj -c Release

# Start the server again
dotnet run --configuration Release --no-build --urls "http://localhost:5239"
```

### Option 2: Pull + Build in One Command

```powershell
cd c:\Users\Caleb\Desktop\FocusDeck; git pull origin master; dotnet build .\src\FocusDeck.Server\FocusDeck.Server.csproj -c Release; dotnet run --configuration Release --no-build
```

---

## Production Update (Linux Server)

If you have FocusDeck deployed on a Linux server, use this process:

### Step 1: SSH into Your Server

```bash
ssh user@your-server-ip
```

### Step 2: Navigate to FocusDeck Directory

```bash
cd ~/FocusDeck
# or wherever you installed it
```

### Step 3: Pull the Latest Changes

```bash
git pull origin master
```

### Step 4: Stop the Running Service

```bash
# If using systemd
sudo systemctl stop focusdeck

# Or kill the process directly
pkill -f "dotnet.*FocusDeck.Server"
```

### Step 5: Build the Updated Version

```bash
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
```

### Step 6: Start the Service

```bash
# If using systemd
sudo systemctl start focusdeck

# Or run manually
dotnet ~/focusdeck-server/FocusDeck.Server.dll
```

### Step 7: Verify It's Running

```bash
# Check the service status
sudo systemctl status focusdeck

# Or test the health endpoint
curl http://localhost:5239/healthz
```

---

## Docker Update (If Using Docker)

If you're running FocusDeck in Docker:

```bash
# Pull latest changes
cd ~/FocusDeck
git pull origin master

# Rebuild the Docker image
docker build -t focusdeck:latest .

# Stop the old container
docker stop focusdeck-server
docker rm focusdeck-server

# Start the new container
docker run -d \
  --name focusdeck-server \
  -p 5239:5239 \
  -v focusdeck-data:/app/data \
  focusdeck:latest
```

---

## What Changed in This Update?

The following files were modified:

### 1. **app.js** - Web UI Authentication
- Added automatic JWT token generation
- Tokens stored in browser localStorage
- All API calls now include Authorization headers
- This fixes the 401 Unauthorized errors

### 2. **index.html** - Favicon
- Added embedded SVG favicon
- Eliminates the 404 favicon.ico error

### 3. **Program.cs** - Hangfire Configuration
- Added conditional Hangfire setup for PostgreSQL
- Added StubBackgroundJobClient for SQLite development
- Allows the server to start without PostgreSQL

### 4. **StubBackgroundJobClient.cs** (New File)
- Implements IBackgroundJobClient interface
- Allows job-dependent services to work in development mode

---

## Testing the Update

Once the server is updated and running:

### 1. Open the Web UI

```
http://localhost:5239/
```
(Or your server's IP/domain)

### 2. Check Browser Console

1. Right-click → Inspect → Console tab
2. You should **NOT** see any 401 errors anymore
3. All API calls should return 200/201 responses

### 3. Test Dashboard Data

- **Notes** should load
- **Automations** should load
- **Services** should load
- **Study Sessions** should display

### 4. Create a Note (API Test)

1. Go to Notes tab
2. Create a new note
3. Should save successfully to the database

---

## Rollback (If Needed)

If something goes wrong, you can revert to the previous version:

### On Your Local Machine

```powershell
cd c:\Users\Caleb\Desktop\FocusDeck

# View recent commits
git log --oneline -5

# Revert to previous commit (replace HASH with the previous commit ID)
git revert HEAD

# Push the revert
git push origin master
```

### On Your Server

```bash
cd ~/FocusDeck

# Revert to previous version
git revert HEAD

# Rebuild and restart
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
sudo systemctl restart focusdeck
```

---

## Troubleshooting

### Server Won't Start After Update

1. **Check .NET version**:
   ```powershell
   dotnet --version
   # Should be 9.0 or higher
   ```

2. **Clear build cache**:
   ```powershell
   cd c:\Users\Caleb\Desktop\FocusDeck
   dotnet clean
   dotnet build .\src\FocusDeck.Server\FocusDeck.Server.csproj -c Release
   ```

3. **Check if port is in use**:
   ```powershell
   netstat -ano | findstr :5239
   ```

4. **Check logs** (if running as service):
   ```bash
   journalctl -u focusdeck -f  # Linux
   ```

### API Calls Still Return 401

1. **Clear browser cache**:
   - Ctrl+Shift+Delete (or Cmd+Shift+Delete on Mac)
   - Clear all cookies and localStorage

2. **Hard refresh the page**:
   - Ctrl+Shift+R (or Cmd+Shift+R on Mac)

3. **Check browser console**:
   - Look for error messages
   - Check that token is being generated in localStorage

### Database Issues

1. **Reset the database** (development only):
   ```bash
   rm ~/focusdeck.db
   dotnet run --configuration Release
   ```

2. **Check database file permissions** (Linux):
   ```bash
   ls -la ~/focusdeck.db
   chmod 666 ~/focusdeck.db
   ```

---

## Next Steps

1. ✅ Pull the latest code from GitHub
2. ✅ Rebuild the project
3. ✅ Restart your server
4. ✅ Open the web UI and verify 401 errors are gone
5. ✅ Test creating/editing notes, automations, and other features

Your server is now updated with JWT authentication for the web UI! 🎉

