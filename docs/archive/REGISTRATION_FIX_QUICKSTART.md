# Quick Fix for Registration 500 Error

## What I Fixed
Enhanced error logging in the PAKE registration endpoint to help diagnose why registration is failing with a 500 error when users try to create accounts.

## What to Do Next

### 1. Pull the Latest Changes
```bash
cd /root/FocusDeck
git pull origin phase-1
```

### 2. Rebuild and Redeploy
```bash
# Build
dotnet build FocusDeck.sln -c Release

# Publish (stop the server first if it's running)
pkill -f "dotnet.*FocusDeck.Server"
dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj -c Release \
  -o /home/focusdeck/FocusDeck/publish

# Restart the server
systemctl restart focusdeck
# or manually:
# su - focusdeck
# cd ~/FocusDeck && nohup dotnet publish/FocusDeck.Server.dll > server.log 2>&1 &
```

### 3. Test Registration Again
Try registering a new account. If you still get a 500 error, check the server logs:

```bash
# View the server logs (adjust path based on your setup)
tail -f /var/log/focusdeck/app.log
# or
tail -f /home/focusdeck/FocusDeck/logs/server.log
```

The enhanced logging will show you the exact error that's causing the 500.

## What the Fix Does

The code now logs:
- The actual exception message
- The exception type (e.g., DbUpdateException, InvalidOperationException)
- The full stack trace
- Inner exception details
- The user ID and remote IP making the request
- The specific database operation that failed

This will help identify if the issue is:
- Missing database migrations
- Database connection problems
- Schema mismatches
- Data validation issues
- Or something else entirely

## Browser Warnings (Not Related to 500 Error)

The Cloudflare and integrity hash warnings you're seeing are browser-side issues with Enhanced Tracking Protection. These don't cause the 500 error but you can:

1. **Disable Firefox Enhanced Tracking Protection** for your domain
2. **Try a different browser** (Chrome, Safari, Edge)
3. **Use incognito/private mode** if Enhanced Tracking Protection settings persist

## Still Having Issues?

Share the error message from the server logs and I can help diagnose further. The enhanced logging should make it much clearer what's going wrong.

