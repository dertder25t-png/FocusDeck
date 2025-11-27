# UseDefaultFiles Fix - Login Page Now Accessible

## Issue
The login page was not accessible at `/login` or `/` because the SPA middleware was not properly configured. The root cause was **missing `app.UseDefaultFiles()` middleware** in `Program.cs`.

## Problem Explained
- `UseStaticFiles()` alone serves files from `wwwroot/` but **does not** automatically serve `index.html` when accessing `/`
- `UseDefaultFiles()` tells ASP.NET Core to serve `index.html` as the default file for directory requests
- Without it, requests to `/` returned 404 instead of the React app
- Requests to `/login` also returned 404 because React routing happens *inside* the loaded app

## Solution
Added one line before `UseStaticFiles()` in `Program.cs`:

```csharp
// Serve index.html as default file (enables SPA routing)
app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // ... cache headers ...
    }
});
```

## Deployment Instructions

### On Linux Server (192.168.1.110)

1. **SSH into server**
```bash
ssh focusdeck@192.168.1.110
```

2. **Pull latest code**
```bash
cd ~/FocusDeck
git pull origin phase-1
```

3. **Verify commit pulled**
```bash
git log --oneline -1
# Should show: c7b1eb2 Fix: Add missing UseDefaultFiles() middleware for SPA routing
```

4. **Rebuild and deploy**
```bash
# Clean old build
rm -rf src/FocusDeck.Server/{bin,obj,wwwroot}
rm -rf src/FocusDeck.WebApp/dist

# Build
dotnet clean
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build

# Stop service
sudo systemctl stop focusdeck

# Deploy
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/

# Verify files were copied
ls -la /opt/focusdeck/wwwroot/ | head -10

# Start service
sudo systemctl start focusdeck

# Check status
sudo systemctl status focusdeck --no-pager
```

5. **Test locally**
```bash
# Test direct to localhost:5000
curl -I http://localhost:5000/
curl -I http://localhost:5000/login

# Should return 200 OK with text/html content
```

6. **Test via Cloudflare**
```bash
# In browser:
http://192.168.1.110:5000
https://focusdeckv1.909436.xyz
```

### Verify Fix

✅ **Root handler serves SPA:**
- `GET /` → 200 OK, returns index.html
- `GET /login` → 200 OK, returns index.html (React handles routing)

✅ **React routing works:**
- Page loads at `http://192.168.1.110:5000/login`
- Login form renders properly
- Authentication flow proceeds

✅ **Static assets cached:**
- JS/CSS files → 7-day cache headers
- HTML files → no-cache headers (for Cloudflare)

## Files Changed
- `src/FocusDeck.Server/Program.cs` - Added `app.UseDefaultFiles();` before `UseStaticFiles()`

## Commit Hash
- `c7b1eb2` - Fix: Add missing UseDefaultFiles() middleware for SPA routing

## Testing Checklist
- [ ] Server pulls latest code (git log shows c7b1eb2)
- [ ] Rebuild completes without errors
- [ ] `/opt/focusdeck/wwwroot/` contains index.html and assets/
- [ ] `curl http://localhost:5000/` returns 200 OK
- [ ] `curl http://localhost:5000/login` returns 200 OK
- [ ] Browser: `http://192.168.1.110:5000/` shows login page
- [ ] Browser: `https://focusdeckv1.909436.xyz` shows login page
- [ ] Login form renders and is interactive
