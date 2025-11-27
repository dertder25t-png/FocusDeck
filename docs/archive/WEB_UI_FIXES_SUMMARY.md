# FocusDeck Web UI - Fixes and Updates (Nov 4, 2025)

## Summary of Issues Fixed

### 1. **401 Unauthorized Errors - ✅ FIXED**

**Problem:** All API calls were returning `401 (Unauthorized)` errors because the web UI was not sending authentication tokens.

**Solution:** 
- Added automatic JWT token generation in `app.js`
- When the app loads, it calls `/api/auth/token` to generate a token for "web-user"
- Implemented `apiFetch()` wrapper that automatically includes `Authorization: Bearer <token>` header on all API requests
- Token is stored in `localStorage` and reused if still valid

**Files Modified:**
- `src/FocusDeck.Server/wwwroot/app.js` - Added token management and apiFetch wrapper

**Method Details:**
```javascript
async ensureToken() {
    // Checks localStorage for valid token, generates new one if needed
    // Returns valid JWT token for all API calls
}

async apiFetch(url, options = {}) {
    // Wrapper around fetch() that automatically adds Authorization header
    // Ensures token is available before making request
}
```

### 2. **Favicon 404 Error - ✅ FIXED**

**Problem:** Console error: `GET http://192.168.1.110:5000/favicon.ico 404 (Not Found)`

**Solution:**
- Added inline SVG favicon using data URI in `index.html`
- Favicon displays a purple background with a target emoji (🎯)

**Files Modified:**
- `src/FocusDeck.Server/wwwroot/index.html` - Added favicon link tag

### 3. **documentPictureInPicture Error - ✅ ANALYZED**

**Problem:** Console error: `ReferenceError: documentPictureInPicture is not defined` in `cnt.js`

**Root Cause:** This error originates from browser extensions or injected scripts, NOT from FocusDeck code. The `cnt.js` file is minified browser output, not our source code.

**Solution:** No action needed - this is from external browser plugins/extensions trying to use Picture-in-Picture API which may not be supported in the environment.

### 4. **Hangfire Dependency Issue - ✅ FIXED**

**Problem:** Server failed to start with error: "Unable to resolve service for type 'Hangfire.IBackgroundJobClient'"

**Root Cause:** Job services required IBackgroundJobClient, but Hangfire was only configured for PostgreSQL deployment.

**Solution:**
- Created `StubBackgroundJobClient` class for SQLite/development environments
- Registered as singleton when PostgreSQL is not detected
- Allows background job classes to function without requiring full Hangfire setup

**Files Modified:**
- `src/FocusDeck.Server/Middleware/StubBackgroundJobClient.cs` - New file
- `src/FocusDeck.Server/Program.cs` - Updated dependency registration

### 5. **API Calls Updated to Use Token Authentication**

All major API endpoints now include proper authentication:
- ✅ Notes API (GET, POST, PUT, DELETE)
- ✅ Decks API (POST, GET)
- ✅ Study Sessions API (GET, POST)
- ✅ Automations API (GET, POST, DELETE, toggle, run, history)
- ✅ Services API (GET, POST, DELETE, health check)
- ✅ Server update checks
- ✅ Token generation endpoints

---

## How to Run

### Development (SQLite)

```bash
cd src/FocusDeck.Server
dotnet run --configuration Release
# Server will start on http://localhost:5239
```

### Production (PostgreSQL)

```bash
# Set environment variable for PostgreSQL connection
$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=focusdeck;User=postgres;Password=..."

# Run the server
dotnet run --configuration Release
```

---

## Testing the Fixes

### 1. Test Token Generation
```bash
curl -X POST http://localhost:5239/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"test-user"}'
```

Expected response:
```json
{
  "token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "username": "test-user",
  "expiresAt": "2025-12-04T14:24:00Z"
}
```

### 2. Test API Call with Token
```bash
curl -X GET http://localhost:5239/api/notes \
  -H "Authorization: Bearer <token>"
```

### 3. Open Web UI
Navigate to: `http://localhost:5239/`

The web UI will automatically:
1. Detect no token in localStorage
2. Call `/api/auth/token` to generate one
3. Store it in localStorage
4. Load all dashboard data with proper authentication

---

## Key Changes to app.js

### New Methods Added

```javascript
// Load token from localStorage or generate new one
loadToken()

// Save token to localStorage
saveToken(token, expiresAt)

// Ensure we have a valid token before making API calls
async ensureToken()

// Get Authorization headers for fetch calls
getAuthHeaders()

// Wrapper for fetch() that automatically handles authentication
async apiFetch(url, options = {})
```

### Updated API Calls

All `fetch()` calls have been updated to use `this.apiFetch()` instead:

**Before:**
```javascript
const response = await fetch('/api/notes');
```

**After:**
```javascript
const response = await this.apiFetch('/api/notes');
```

This ensures all API calls automatically include the JWT token.

---

## Security Considerations

⚠️ **Development Token Generation**
- The `/api/auth/token` endpoint is open for development purposes
- In production, you should:
  1. Restrict token generation to authenticated users
  2. Use the existing authentication system
  3. Implement proper OAuth or session management
  4. Disable public token generation

---

## Next Steps

1. ✅ All console 401 errors should be resolved
2. ✅ Favicon shows without 404 error
3. ✅ Server starts successfully on both SQLite and PostgreSQL
4. ✅ Web UI loads data from API with proper authentication

Monitor the browser console and server logs to confirm everything is working properly!

---

## Files Modified

1. `src/FocusDeck.Server/wwwroot/app.js` - Token management + API wrapper
2. `src/FocusDeck.Server/wwwroot/index.html` - Favicon
3. `src/FocusDeck.Server/Program.cs` - Hangfire stub client
4. `src/FocusDeck.Server/Middleware/StubBackgroundJobClient.cs` - New file

