# FocusDeck Update System

## Overview

The FocusDeck server now includes a comprehensive update system that allows you to check for updates and automatically update the server with a single click from the web interface.

## Features

### ✅ Check for Updates
- **Button**: "Check for Updates" in Settings → Server Management
- **Function**: Queries GitHub API to compare your current commit with the latest master branch commit
- **Display**: Shows current version (commit hash + date) and available updates

### 🔄 One-Click Update
- **Button**: "Update Server Now" (appears only when updates are available)
- **Process**: 
  1. Pulls latest code from GitHub
  2. Rebuilds the application
  3. Restarts the server
  4. **Auto-reloads the webpage** when server is back online

### 🚀 Auto-Reload After Update
- The webpage automatically detects when the server is back online
- Health checks ping `/api/server/health` every second
- Page reloads automatically within 2 seconds of server being ready
- 60-second timeout with manual refresh option as fallback

## API Endpoints

### `GET /api/server/check-updates`
Checks GitHub for available updates.

**Response:**
```json
{
  "updateAvailable": true,
  "currentCommit": "7490ffb",
  "currentDate": "2025-10-31T10:30:00Z",
  "latestCommit": "a9d0069",
  "latestDate": "2025-10-31T12:00:00Z",
  "latestMessage": "feat: Add comprehensive check for updates system",
  "message": "Updates available!"
}
```

### `POST /api/server/update`
Initiates the server update process.

**Response:**
```json
{
  "message": "Server update started! The server will restart in about 30 seconds.",
  "logFile": "/var/log/focusdeck/update.log",
  "estimatedTime": "30-60 seconds"
}
```

### `GET /api/server/update-status`
Checks the status of an ongoing update.

**Response:**
```json
{
  "status": "completed",
  "lastUpdate": "2025-10-31 12:05:00 - FocusDeck Server Update Complete",
  "recentLogs": ["...", "..."]
}
```

### `GET /api/server/health`
Health check endpoint for monitoring server status.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-31T12:05:30Z",
  "platform": "Linux 6.1.0-1023-aws",
  "uptime": 3600
}
```

## UI Components

### Settings Page
The Server Management card includes:

1. **Current Version**: Displays current commit hash and date
2. **Update Available** (conditional): Shows latest version when updates exist
3. **Check for Updates Button**: Manually check GitHub for new commits
4. **Update Server Now Button** (conditional): Visible only when updates are available
5. **Update Status Box** (during update): Shows spinner and progress messages

### Update Flow

```
User clicks "Check for Updates"
         ↓
Query GitHub API for latest commit
         ↓
Compare with local commit
         ↓
   Updates Available?
    ↙           ↘
  YES            NO
Show "Update     Show "You're
Server Now"      up to date!"
button
   ↓
User clicks "Update Server Now"
         ↓
Show confirmation dialog
         ↓
Send POST /api/server/update
         ↓
Show status box with spinner
         ↓
Poll /api/server/health every 1s
         ↓
Server back online?
         ↓
Auto-reload page
```

## Technical Implementation

### Backend (ServerController.cs)

```csharp
// Check for updates using GitHub API
[HttpGet("check-updates")]
public async Task<IActionResult> CheckForUpdates()
{
    // 1. Get current local commit hash using git
    // 2. Query GitHub API for latest master commit
    // 3. Compare commit hashes
    // 4. Return update availability status
}

// Health check endpoint
[HttpGet("health")]
public IActionResult HealthCheck()
{
    return Ok(new { 
        status = "healthy",
        timestamp = DateTime.UtcNow,
        platform = RuntimeInformation.OSDescription,
        uptime = Environment.TickCount64 / 1000
    });
}
```

### Frontend (app.js)

```javascript
// Check for updates
async checkForUpdates() {
    const response = await fetch('/api/server/check-updates');
    const result = await response.json();
    
    if (result.updateAvailable) {
        // Show update button and latest version info
        document.getElementById('updateServerBtn').style.display = 'inline-flex';
    }
}

// Update server with auto-reload
async updateServer() {
    // 1. Send update request
    await fetch('/api/server/update', { method: 'POST' });
    
    // 2. Show status box
    document.getElementById('updateStatusBox').style.display = 'block';
    
    // 3. Poll health endpoint every second
    const checkInterval = setInterval(async () => {
        try {
            const health = await fetch('/api/server/health');
            if (health.ok) {
                clearInterval(checkInterval);
                // 4. Auto-reload page
                window.location.reload();
            }
        } catch (err) {
            // Server still restarting...
        }
    }, 1000);
}
```

## Requirements

- **Platform**: Linux only (update script uses bash, systemctl, etc.)
- **Git Repository**: Must be a git repository with remote origin
- **Permissions**: Service must have permissions to:
  - Pull from git
  - Build with dotnet
  - Restart systemctl service
  - Write to log directory

## Environment Variables

- `FOCUSDECK_REPO`: Path to git repository (default: `/home/focusdeck/FocusDeck`)

## Log Files

Updates are logged to: `/var/log/focusdeck/update.log`

View logs:
```bash
tail -f /var/log/focusdeck/update.log
```

## Safety Features

1. **Confirmation Dialog**: User must confirm before update starts
2. **Timeout Protection**: 60-second timeout with manual refresh option
3. **Error Handling**: Graceful error messages if update fails
4. **Rollback**: Git reset --hard ensures clean state
5. **Status Tracking**: Update status logged to file for debugging

## Future Enhancements

- [ ] Add version tagging system
- [ ] Show changelog/release notes
- [ ] Add rollback to previous version
- [ ] Email notifications on update completion
- [ ] Scheduled automatic updates (opt-in)
- [ ] Pre-update backup creation
- [ ] Windows support for update script

## Troubleshooting

### Update button doesn't appear
- Check that you're on Linux platform
- Verify git repository is properly configured
- Check GitHub API rate limits

### Page doesn't auto-reload
- Check browser console for errors
- Verify `/api/server/health` endpoint is accessible
- Manually refresh after 60 seconds

### Update fails
- Check `/var/log/focusdeck/update.log` for errors
- Verify git permissions: `sudo -u focusdeck git pull`
- Check dotnet is installed and accessible
- Verify systemctl service permissions

## Security Considerations

1. **Authentication**: Add authentication to update endpoints in production
2. **Rate Limiting**: GitHub API has rate limits (60 requests/hour unauthenticated)
3. **HTTPS**: Always use HTTPS in production
4. **Input Validation**: Update script validates paths and commands
5. **Logging**: All update actions are logged for audit trail

---

**Last Updated**: October 31, 2025  
**Version**: 1.1.0
