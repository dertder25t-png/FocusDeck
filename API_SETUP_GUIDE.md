# 🔑 API Setup Guide

**Critical Setup Document** | **Last Updated:** October 28, 2025 | **Version:** 2.0

## 📋 Overview

This guide covers setting up authentication for cloud sync providers. You need to configure **at least ONE** of these:
- ✅ **Microsoft OneDrive** (Recommended - easy setup)
- ✅ **Google Drive** (Alternative)

---

## 🏢 Option 1: Microsoft OneDrive Setup (RECOMMENDED)

### Step 1: Register Application

1. Go to [Azure Portal](https://portal.azure.com)
2. Sign in with your Microsoft account
3. Navigate to **Azure Active Directory** → **App registrations**
4. Click **New registration**

```
Name: FocusDeck Mobile App
Supported account types: Accounts in any organizational directory (Any Azure AD + personal accounts)
Redirect URI: 
  Platform: Mobile and desktop applications
  URI: http://localhost
  
[Register]
```

### Step 2: Get Credentials

After registration, copy these values:
- **Application (client) ID** - You'll see this on Overview tab
- **Tenant ID** - Also on Overview tab (should be "common" or your tenant)

**Example:**
```
Application (client) ID: 1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p
Tenant ID: common
```

### Step 3: Create Client Secret

1. Go to **Certificates & secrets** tab
2. Click **New client secret**
3. Set expiration to 24 months
4. Copy the secret value **immediately** (you won't see it again)

**Example Secret:**
```
Value: Abc~D.EFGhIjkL-mnopQRst_UVWxyz123456
```

### Step 4: Configure API Permissions

1. Go to **API permissions** tab
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Delegated permissions**
5. Search and select:
   - `Files.ReadWrite` (Read and write access to files)
   - `Files.ReadWrite.All` (Read and write all files)
   - `offline_access` (Maintain access offline)

6. Click **Grant admin consent** (if available)

### Step 5: Update Application Code

**File:** `src/FocusDock.Core/Services/OneDriveProvider.cs`

```csharp
public class OneDriveProvider : ICloudProvider
{
    private const string CLIENT_ID = "YOUR_APPLICATION_CLIENT_ID";
    private const string TENANT_ID = "common";
    private const string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
    private const string REDIRECT_URI = "http://localhost";
    
    // TODO: Implement OAuth2 token exchange
    // See: docs/OAUTH2_SETUP.md for details
}
```

Replace:
- `YOUR_APPLICATION_CLIENT_ID` with your Application (client) ID
- `YOUR_CLIENT_SECRET` with your Client Secret

**For Production:** Use secure storage (not hardcoded). See Security Best Practices below.

### Step 6: Test Connection

```bash
# After updating credentials, rebuild
dotnet build src/FocusDeck.Core
dotnet build src/FocusDock.App
```

Expected: Build succeeds, 0 errors

---

## 🔍 Option 2: Google Drive Setup

### Step 1: Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Click **Select a Project** → **New Project**

```
Project name: FocusDeck
Organization: [Leave blank or select your org]
Location: [Leave blank]

[Create]
```

### Step 2: Enable Google Drive API

1. In Google Cloud Console, search for **Google Drive API**
2. Click the result and select **Enable**
3. You should see "API enabled" confirmation

### Step 3: Create OAuth 2.0 Credentials

1. Go to **Credentials** tab
2. Click **Create Credentials** → **OAuth client ID**
3. If prompted, click **Configure OAuth consent screen**

```
Consent Screen Setup:
└─ User type: External
└─ App name: FocusDeck
└─ User support email: [your email]
└─ Developer contact: [your email]
[Save & Continue]
```

4. Return to Credentials, click **Create Credentials** → **OAuth client ID**

```
Application type: Desktop application
Name: FocusDeck Desktop

[Create]
```

### Step 4: Get Credentials

A dialog shows your credentials:
- **Client ID** - Copy this
- **Client Secret** - Copy this
- Download JSON for backup

**Example:**
```json
{
  "installed": {
    "client_id": "123456789-abc.apps.googleusercontent.com",
    "project_id": "focusdeck-123456",
    "client_secret": "GOCSPX-AbCdEfGhIjKlMnOpQrStUvWxYz"
  }
}
```

### Step 5: Update Application Code

**File:** `src/FocusDock.Core/Services/GoogleDriveProvider.cs`

```csharp
public class GoogleDriveProvider : ICloudProvider
{
    private const string CLIENT_ID = "YOUR_CLIENT_ID.apps.googleusercontent.com";
    private const string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
    private const string REDIRECT_URI = "http://localhost";
    
    // TODO: Implement OAuth2 token exchange
    // See: docs/OAUTH2_SETUP.md for details
}
```

Replace:
- `YOUR_CLIENT_ID` with your Client ID
- `YOUR_CLIENT_SECRET` with your Client Secret

### Step 6: Test Connection

```bash
dotnet build src/FocusDeck.Core
dotnet build src/FocusDock.App
```

Expected: Build succeeds, 0 errors

---

## 🔒 Security Best Practices

### ⚠️ DO NOT:
```csharp
// ❌ WRONG - Never hardcode secrets
public const string CLIENT_SECRET = "Abc~D.EFGhIjkL-mnopQRst_UVWxyz123456";

// ❌ WRONG - Never commit secrets to git
git add OneDriveProvider.cs  // If it contains secrets
```

### ✅ DO:

#### Option A: Use User Secrets (Development)
```bash
cd src/FocusDeck.Core

# Set secrets
dotnet user-secrets set "OAuth:OneDrive:ClientId" "your-client-id"
dotnet user-secrets set "OAuth:OneDrive:ClientSecret" "your-client-secret"

# List secrets
dotnet user-secrets list
```

#### Option B: Use Configuration Files (Development)
```json
{
  "OAuth": {
    "OneDrive": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

Add to `.gitignore`:
```
appsettings.Development.json
```

#### Option C: Use Azure Key Vault (Production)
```csharp
var keyVaultUrl = "https://focusdeck-keyvault.vault.azure.net/";
var credentials = new DefaultAzureCredential();
var client = new SecretClient(new Uri(keyVaultUrl), credentials);

var clientSecret = await client.GetSecretAsync("OneDriveClientSecret");
CLIENT_SECRET = clientSecret.Value.Value;
```

---

## 🧪 Testing Your Setup

### Test 1: Verify Credentials Work

**File:** `src/FocusDeck.Core/Tests/CloudProviderTests.cs`

```csharp
[TestClass]
public class CloudProviderTests
{
    [TestMethod]
    public async Task OneDriveProvider_ShouldAuthenticate()
    {
        var provider = new OneDriveProvider();
        var isAuthed = await provider.AuthenticateAsync();
        Assert.IsTrue(isAuthed);
    }
}
```

Run:
```bash
dotnet test src/FocusDeck.Core
```

### Test 2: List Files from Cloud

```csharp
var provider = new OneDriveProvider();
await provider.AuthenticateAsync();

var files = await provider.ListFilesAsync("/FocusDeck");
foreach (var file in files)
{
    Console.WriteLine($"File: {file.Name}");
}
```

---

## 🚨 Troubleshooting

| Issue | Solution |
|-------|----------|
| "Invalid client_id" | Check you copied the Client ID correctly from portal |
| "Redirect URI mismatch" | Ensure `REDIRECT_URI` in code matches registered URI |
| "Insufficient permissions" | Grant required scopes: `Files.ReadWrite offline_access` |
| "Invalid client secret" | Copy secret immediately after creation (appears only once) |
| "Access token expired" | Implement refresh token flow (see OAUTH2_SETUP.md) |

---

## 📝 Completion Checklist

Before starting Phase 6b:

### OneDrive Setup ✅
- [ ] Registered app in Azure Portal
- [ ] Copied Client ID and Secret
- [ ] Configured API permissions
- [ ] Updated OneDriveProvider.cs
- [ ] Stored credentials securely (not hardcoded)
- [ ] Build succeeds

### Google Drive Setup (Optional) ✅
- [ ] Created Google Cloud Project
- [ ] Enabled Google Drive API
- [ ] Created OAuth credentials
- [ ] Updated GoogleDriveProvider.cs
- [ ] Stored credentials securely
- [ ] Build succeeds

---

## 🔗 Canvas LMS Integration Setup

### Step 1: Get Your Canvas API Token

1. Log into your Canvas instance (e.g., https://yourschool.instructure.com)
2. Click your **Profile** (user icon in top right)
3. Click **Settings**
4. Scroll down to **Approved Integrations**
5. Click **+ New Access Token**
6. Give it a name like "FocusDeck"
7. Leave the expiration date blank (or set it far in the future)
8. Click **Generate Token**
9. **Copy the token** - you won't be able to see it again!

### Step 2: Find Your Canvas Instance URL

Your Canvas instance URL is the base URL you use to access Canvas:
- Example: `https://myschool.instructure.com`
- Or: `https://canvas.company.com`

### Step 3: Configure FocusDeck

When you open FocusDeck and navigate to Settings → Canvas:

1. Paste your **Canvas Instance URL** (e.g., `https://myschool.instructure.com`)
2. Paste your **API Token** in the Canvas API Token field
3. Check **Enable Canvas**
4. Click **Test Connection** to verify it works
5. If successful, click **Save**

FocusDeck will now:
- Fetch all your Canvas courses
- Detect assignments from those courses
- Automatically create tasks for upcoming assignments
- Show assignment due dates in the calendar
- Sync every 15 minutes (configurable)

---

## What Happens After Setup

### Google Calendar
- **Automatic**: Every 15 minutes, FocusDeck fetches your Google Calendar events
- **Display**: Upcoming events appear in the Calendar view
- **Smart Layouts**: You can set a layout to automatically apply when specific calendar events start
- **Reminders**: Notifications 5 minutes (configurable) before events

### Canvas Assignments
- **Automatic**: Every 15 minutes, FocusDeck fetches your Canvas assignments
- **To-Do List**: Assignments automatically become tasks in your to-do list
- **Calendar**: Due dates appear on the calendar
- **Priority**: Overdue assignments appear first with visual indicators
- **Tracking**: Mark assignments as complete when submitted

### Combined Features
- Calendar events and assignments appear together in timeline view
- Study sessions automatically block out time before due dates
- Effectiveness ratings help identify your most productive times
- Workspaces can be triggered by specific calendar events

---

## Troubleshooting

### "Test Connection Failed" for Canvas
- Verify your instance URL is correct (no trailing slash)
- Confirm the API token hasn't expired
- Check that you copied the entire token without extra spaces
- Ensure your Canvas user has permission to access courses

### Google Calendar Not Syncing
- Verify your Client ID and Secret are correct
- Check that the Google Calendar API is enabled in Google Cloud Console
- Re-authorize: Click "Authorize with Google" again
- Check your Google account permissions at [myaccount.google.com/permissions](https://myaccount.google.com/permissions)

### Assignments Not Appearing
- Click "Sync Now" in the Tasks view to force an immediate sync
- Verify you're enrolled in at least one Canvas course
- Check that assignments have due dates set
- Ensure your Canvas user role can view assignments

### Can't Find Settings
In FocusDeck:
1. Look for a ⚙️ Settings icon or menu
2. Or click the FocusDeck icon in taskbar and select "Settings"
3. Look for "Calendar" or "API" tab

---

## Privacy & Security Notes

✅ **What FocusDeck does:**
- Stores tokens locally in `%LOCALAPPDATA%\FocusDeck\calendar_settings.json`
- Only syncs events/assignments you have access to
- Never stores your calendar data on external servers

❌ **What FocusDeck does NOT do:**
- Never sends your data to third-party servers
- Never modifies your calendar events (read-only)
- Never modifies Canvas assignments (read-only for status tracking)
- Never shares your tokens with anyone

---

## Revoking Access

### Google Calendar
1. Go to [myaccount.google.com/permissions](https://myaccount.google.com/permissions)
2. Find "FocusDeck" in your connected apps
3. Click it and select "Remove access"

### Canvas
1. Log into Canvas
2. Go to **Profile → Settings → Approved Integrations**
3. Find your "FocusDeck" token
4. Click the trash icon to delete it

---

## Next Steps

After setting up both integrations:

1. **Try the Study Session Tracker**: Start a study session and watch your effectiveness rating
2. **Set Up Automations**: Configure time-based rules in Settings → Automations
3. **Create Study Workspaces**: Design layouts for different subjects
4. **Explore Calendar Views**: Check out the calendar UI with your real events
5. **Fine-Tune Sync**: Adjust sync interval if you want more/less frequent updates

Questions? Check the main README.md for the full feature list and project status.
