# Update Server to Get Notes Feature

## Problem
The Notes button is missing from your Linux server's web UI even though it exists in the code.

## Root Cause
When you update the server, the `wwwroot` static files (HTML, CSS, JS) need to be published and deployed. A simple `git pull` updates the source code but doesn't deploy the built files.

## Solution: Properly Update Your Server

### Option 1: Use the Web UI Update Button (Recommended)

1. Go to your FocusDeck web UI → **Settings** → **Server Management**
2. Click **"Update Server Now"**
3. This will:
   - Run `git pull origin master`
   - Run `dotnet publish` (which copies wwwroot files)
   - Restart the service
4. Wait for the page to auto-reload
5. The **📝 Notes** button should now appear!

### Option 2: Manual SSH Command

If the web update doesn't work, SSH into your server and run:

```bash
cd ~/FocusDeck
git pull origin master
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained true -o ~/focusdeck-server
sudo systemctl restart focusdeck
```

**Important:** The `dotnet publish` step is crucial - it copies all the wwwroot files to the deployment directory.

### Option 3: Quick wwwroot-only update

If you just want to update the HTML/CSS/JS without rebuilding:

```bash
cd ~/FocusDeck/src/FocusDeck.Server
cp -r wwwroot/* ~/focusdeck-server/wwwroot/
sudo systemctl restart focusdeck
```

## After Update

Clear your browser cache (Ctrl+F5 or Cmd+Shift+R) and you should see:

- **📝 Notes** button in the sidebar (between Study Timer and Decks)
- Clicking it opens a full note-taking interface with search, tags, and pinning

## Verification

Check your server's deployed HTML file:
```bash
grep -n "data-view=\"notes\"" ~/focusdeck-server/wwwroot/index.html
```

If this shows no results, the wwwroot files weren't properly deployed.

## Why This Happened

Your git repository has the latest code, but the **running server** uses the compiled output in `~/focusdeck-server/`. The `dotnet publish` command is what copies the wwwroot static files from source to the deployment directory.
