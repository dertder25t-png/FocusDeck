# Registration 500 Error - Fix Summary

## Issue
Users were experiencing HTTP 500 errors when attempting to register accounts via the `/v1/auth/pake/register/finish` endpoint.

**Error Logs:**
```
POST https://focusdeckv1.909436.xyz/v1/auth/pake/register/finish
[HTTP/2 500  192ms]
```

Related browser warnings (secondary):
- Cross-Origin Request Blocked for Cloudflare Insights script
- Enhanced Tracking Protection blocking requests
- Integrity hash mismatch warnings

## Root Cause Analysis

The 500 error occurs when the server tries to save `PakeCredential` and `KeyVault` entities to the database during user registration. The actual root cause could be:

1. **Missing Database Migrations**: The deployed database may not have applied the migrations that added the `TenantId` column
2. **Unhandled Database Exception**: An exception occurs during `SaveChangesAsync()` that wasn't being properly logged
3. **Connection Issue**: The database connection string may be misconfigured on the deployed server
4. **Schema Mismatch**: The deployed database schema might be out of sync with the code

## Solution Implemented

### 1. Enhanced Error Logging
Modified `/root/FocusDeck/src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs` to provide detailed error diagnostics:

```csharp
catch (Exception ex)
{
    var userId = request?.UserId;
    var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    _logger.LogError(ex, "PAKE register finish unhandled exception for {UserId} from {RemoteIp}: {Message} | StackTrace: {StackTrace}", 
        userId, remoteIp, ex.Message, ex.StackTrace);
    await LogAuthEventAsync("PAKE_REGISTER_FINISH", userId, false, "Exception", metadataJson: JsonSerializer.Serialize(new { 
        exception = ex.Message ?? "unknown",
        exceptionType = ex.GetType().Name,
        stackTrace = ex.StackTrace
    }));
    TrackRegisterFailure("exception", userId, null, remoteIp);
    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
}
```

And added more detailed logging around the database save:

```csharp
try
{
    await _db.SaveChangesAsync();
}
catch (Exception dbEx)
{
    _logger.LogError(dbEx, "Database SaveChangesAsync failed for user {UserId}. InnerException: {InnerMessage}", 
        request.UserId, dbEx.InnerException?.Message);
    throw; // Re-throw to be caught by outer catch block
}
```

### 2. Verification
- ✅ Unit tests pass (`Pake_Register_Login_VaultRoundTrip`)
- ✅ Database schema is correct (TenantId column exists)
- ✅ Direct SQLite inserts succeed
- ✅ Code properly initializes TenantId with `Guid.NewGuid()`

## Deployment Steps

1. **Build the solution:**
   ```bash
   cd /root/FocusDeck
   dotnet build FocusDeck.sln -c Release
   ```

2. **Publish the server:**
   ```bash
   dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj -c Release \
     -o /home/focusdeck/FocusDeck/publish
   ```

3. **Restart the application server:**
   ```bash
   pkill -f "dotnet.*FocusDeck.Server"
   systemctl restart focusdeck  # or your deployment method
   ```

4. **Verify migrations are applied:**
   The `Startup.cs` Configure method automatically runs migrations on startup:
   ```csharp
   db.Database.Migrate();
   ```

## Troubleshooting

If you still encounter registration errors after deployment, check:

### 1. Database Connectivity
```bash
sqlite3 /home/focusdeck/FocusDeck/data/focusdeck.db ".tables"
```

### 2. Schema Verification
```bash
sqlite3 /home/focusdeck/FocusDeck/data/focusdeck.db ".schema PakeCredentials"
sqlite3 /home/focusdeck/FocusDeck/data/focusdeck.db ".schema KeyVaults"
```

Should output:
```sql
CREATE TABLE "PakeCredentials" (
    "UserId" TEXT NOT NULL,
    ...
    "TenantId" TEXT NOT NULL
);

CREATE TABLE "KeyVaults" (
    "UserId" TEXT NOT NULL,
    ...
    "TenantId" TEXT NOT NULL
);
```

### 3. Server Logs
Check the application logs for the detailed error:
```bash
tail -f /path/to/logs/app.log | grep -i "pake\|register\|database"
```

The enhanced logging will show:
- Exception type
- Exception message
- Stack trace
- Inner exception details

### 4. Configuration Validation
Verify `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/home/focusdeck/FocusDeck/data/focusdeck.db"
  }
}
```

## Related Issues Addressed

### Browser-Side Warnings (Secondary)
The Cloudflare and integrity hash warnings are browser-specific and not related to the registration failure:

1. **Enhanced Tracking Protection**: Firefox blocks third-party tracking scripts
   - Solution: Users can disable ETP for the domain or use a browser without strict ETP

2. **Integrity Hash Mismatch**: The Cloudflare script hash doesn't match
   - This is typically caused by Cloudflare's DDoS protection rewriting the script
   - Solution: Disable Cloudflare script integrity checking or allow it through CFbypass

## Testing the Fix

After deployment, test registration with:

```bash
# 1. Start registration
curl -X POST https://your-domain/v1/auth/pake/register/start \
  -H "Content-Type: application/json" \
  -d '{"userId":"test@example.com","devicePlatform":"web"}'

# 2. Complete registration (with valid SRP verifier)
curl -X POST https://your-domain/v1/auth/pake/register/finish \
  -H "Content-Type: application/json" \
  -d '{
    "userId":"test@example.com",
    "verifierBase64":"<valid-srp-verifier>",
    "kdfParametersJson":"<kdf-params-from-start>",
    "vaultDataBase64":null,
    "vaultKdfMetadataJson":null,
    "vaultCipherSuite":null
  }'
```

Expected success response:
```json
{
  "success": true
}
```

## Files Modified

- `/root/FocusDeck/src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs`
  - Enhanced exception logging in `RegisterFinish()` method
  - Added detailed database save error handling

## Commits

```bash
git add src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs
git commit -m "fix(auth): enhance registration error logging for 500 debug"
git push origin phase-1
```

## Next Steps

1. Deploy the updated code
2. Monitor server logs for the next registration attempt
3. If errors persist, the enhanced logging will reveal the actual root cause
4. Reach out with log excerpt if needed

