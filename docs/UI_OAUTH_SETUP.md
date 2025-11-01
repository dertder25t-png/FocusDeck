# UI-Based OAuth Configuration

## Overview

FocusDeck now allows users to configure OAuth services (Spotify, Google Calendar, Google Drive) entirely through the web UI. No more manual `appsettings.json` editing required!

## How It Works

### For Users

1. **Navigate to Integrations** in the web UI
2. **Click on a service** (e.g., Spotify)
3. **Follow the setup guide** with step-by-step instructions
4. **Paste credentials** directly into the form:
   - Client ID
   - Client Secret
5. **Click "Start OAuth Flow"** to authorize
6. **Done!** The service is now connected

### Architecture

#### Database Storage
- OAuth credentials (Client ID, Client Secret, API keys) are stored in the `ServiceConfigurations` table
- Each service has one configuration record (unique index on `ServiceName`)
- Credentials are stored securely in SQLite database

#### API Endpoints

**Save Configuration:**
```http
POST /api/services/{service}/config
Content-Type: application/json

{
  "clientId": "your_client_id",
  "clientSecret": "your_client_secret",
  "apiKey": "optional_api_key"
}
```

**Get Configuration Status:**
```http
GET /api/services/{service}/config

Response:
{
  "configured": true,
  "hasClientId": true,
  "hasClientSecret": true,
  "clientIdPreview": "abc1...xyz9",
  "updatedAt": "2025-11-01T06:30:00Z"
}
```

**Delete Configuration:**
```http
DELETE /api/services/{service}/config
```

#### OAuth Flow

1. User enters Client ID and Client Secret in UI
2. Frontend calls `POST /api/services/{service}/config` to save credentials
3. User clicks "Start OAuth Flow"
4. Frontend calls `GET /api/services/oauth/{service}/url`
5. Backend retrieves credentials from database (or falls back to appsettings.json)
6. Backend generates OAuth URL with redirect URI
7. User authorizes in popup window
8. OAuth provider redirects to `/api/services/oauth/{service}/callback`
9. Backend exchanges code for tokens using stored credentials
10. Access token stored in `ConnectedServices` table

### Credential Priority

The system checks credentials in this order:
1. **Database** (`ServiceConfigurations` table) - preferred
2. **appsettings.json** (fallback for backward compatibility)

This means users can configure services through the UI, or developers can still use appsettings.json for testing.

### Security Features

- Secrets are stored in the database (not in config files)
- GET config endpoint returns masked previews (e.g., "abc1...xyz9")
- Full secrets are never returned in API responses
- Credentials are only read by backend OAuth flow

## Setup Guide Updates

All OAuth services now show:
- **Step-by-step instructions** for creating developer apps
- **Clickable documentation links** to provider dashboards
- **Input fields** for Client ID and Client Secret
- **"Save Configuration" button** (automatic when clicking "Start OAuth Flow")

### Example: Spotify Setup

1. Go to https://developer.spotify.com/dashboard
2. Create an App
3. Set redirect URI to: `http://localhost:5239/api/services/oauth/Spotify/callback`
4. Copy Client ID and Client Secret
5. Paste into FocusDeck UI
6. Click "Start OAuth Flow"
7. Authorize Spotify
8. Done!

## Migration Notes

### Existing Users

If you already have OAuth credentials in `appsettings.json`, they will continue to work! The system falls back to appsettings.json if no database configuration exists.

### New Users

Simply use the UI to configure services. No manual file editing needed.

### Database Schema

The `ServiceConfigurations` table is created automatically by `EnsureCreated()` in Program.cs.

**Table Structure:**
```sql
CREATE TABLE ServiceConfigurations (
    Id TEXT PRIMARY KEY,
    ServiceName TEXT NOT NULL UNIQUE,
    ClientId TEXT,
    ClientSecret TEXT,
    ApiKey TEXT,
    AdditionalConfig TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
```

## Benefits

✅ **User-friendly:** No need to edit config files or restart server  
✅ **Secure:** Credentials stored in database, not in code repository  
✅ **Flexible:** Supports both UI and config file methods  
✅ **Scalable:** Easy to add more OAuth services  
✅ **Home Assistant-style:** Power users can configure everything through UI  

## Future Enhancements

- [ ] Add "Test Connection" button after saving config
- [ ] Show connection status (authorized/unauthorized) in service list
- [ ] Add ability to edit/update credentials without re-authorizing
- [ ] Support for multiple accounts per service
- [ ] Export/import configuration for backups
