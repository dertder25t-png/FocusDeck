# ✅ API Integration Checklist

**Phase 6b Implementation Dependency** | **Last Updated:** October 28, 2025

## 📋 Pre-Phase 6b: Complete This Before Building Mobile App

### Prerequisite: Cloud Provider Credentials

```
⚠️  You MUST complete API_SETUP_GUIDE.md first!

Choose ONE (OneDrive recommended):
  ✅ Option 1: Microsoft OneDrive Setup (5-10 min)
  ✅ Option 2: Google Drive Setup (10-15 min)
```

---

## 🏢 OAuth2 Implementation Checklist

### For OneDrive Provider

#### Step 1: Add Microsoft.Identity NuGet Package
```bash
cd src/FocusDeck.Core

# Install MSAL (Microsoft Authentication Library)
dotnet add package Microsoft.Identity.Client --version 4.57.0
dotnet add package Microsoft.Graph --version 5.40.0
```

#### Step 2: Implement OAuth2 Flow

**File:** `src/FocusDeck.Core/Services/OneDriveProvider.cs`

```csharp
using Microsoft.Identity.Client;

public class OneDriveProvider : ICloudProvider
{
    private const string CLIENT_ID = "YOUR_APPLICATION_CLIENT_ID";
    private const string TENANT_ID = "common";
    private const string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
    private const string[] SCOPES = { "Files.ReadWrite", "offline_access" };
    
    private IPublicClientApplication? _publicClientApp;
    private IAccount? _userAccount;
    private string? _accessToken;
    
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            _publicClientApp = PublicClientApplicationBuilder
                .Create(CLIENT_ID)
                .WithDefaultRedirectUri()
                .Build();
            
            var accounts = await _publicClientApp.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            
            if (account != null)
            {
                // Try silent authentication
                var result = await _publicClientApp.AcquireTokenSilent(SCOPES, account)
                    .ExecuteAsync();
                _accessToken = result.AccessToken;
                _userAccount = account;
                return true;
            }
            else
            {
                // Interactive authentication
                var result = await _publicClientApp.AcquireTokenInteractive(SCOPES)
                    .ExecuteAsync();
                _accessToken = result.AccessToken;
                _userAccount = result.Account;
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Authentication failed: {ex.Message}");
            return false;
        }
    }
    
    public async Task<CloudFileInfo[]> ListFilesAsync(string path)
    {
        if (string.IsNullOrEmpty(_accessToken))
            throw new InvalidOperationException("Not authenticated");
        
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            
            // TODO: Implement Microsoft Graph API call
            // GET https://graph.microsoft.com/v1.0/me/drive/root/children
            
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"List files failed: {ex.Message}");
            throw;
        }
    }
    
    // Implement remaining ICloudProvider methods...
}
```

**Acceptance Criteria:**
- [ ] MSAL NuGet packages installed
- [ ] AuthenticateAsync() shows OAuth dialog
- [ ] Token stored successfully
- [ ] Silent re-authentication works
- [ ] Build succeeds

#### Step 3: Test OAuth Flow

```csharp
// In App.xaml.cs or MainWindow.xaml.cs
public partial class MainWindow : Window
{
    private ICloudProvider _provider = new OneDriveProvider();
    
    private async void OnAuthenticateClick(object sender, RoutedEventArgs e)
    {
        bool success = await _provider.AuthenticateAsync();
        if (success)
        {
            MessageBox.Show("Authenticated with OneDrive!");
            
            // Try listing files
            var files = await _provider.ListFilesAsync("/");
            foreach (var file in files)
            {
                Debug.WriteLine($"File: {file.Name}");
            }
        }
    }
}
```

---

### For Google Drive Provider

#### Step 1: Add Google API NuGet Packages
```bash
cd src/FocusDeck.Core

# Install Google APIs
dotnet add package Google.Apis.Drive.v3 --version 1.66.0
dotnet add package Google.Apis.Auth --version 1.64.0
```

#### Step 2: Implement OAuth2 Flow

**File:** `src/FocusDeck.Core/Services/GoogleDriveProvider.cs`

```csharp
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;

public class GoogleDriveProvider : ICloudProvider
{
    private const string CLIENT_ID = "YOUR_CLIENT_ID.apps.googleusercontent.com";
    private const string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
    private const string REDIRECT_URI = "http://localhost";
    
    private static readonly string[] SCOPES = { DriveService.Scope.Drive };
    
    private DriveService? _driveService;
    private UserCredential? _credential;
    
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = CLIENT_ID,
                    ClientSecret = CLIENT_SECRET
                },
                SCOPES,
                "user",
                CancellationToken.None,
                new FileDataStore("FocusDeckGoogleDrive"));
            
            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "FocusDeck"
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Authentication failed: {ex.Message}");
            return false;
        }
    }
    
    public async Task<CloudFileInfo[]> ListFilesAsync(string path)
    {
        if (_driveService == null)
            throw new InvalidOperationException("Not authenticated");
        
        try
        {
            var request = _driveService.Files.List();
            request.Spaces = "drive";
            request.PageSize = 10;
            request.Q = $"name='{path}' and trashed=false";
            
            var result = await request.ExecuteAsync();
            
            // TODO: Map Google Drive files to CloudFileInfo
            return result.Files
                .Select(f => new CloudFileInfo
                {
                    Id = f.Id,
                    Name = f.Name,
                    Size = f.Size ?? 0,
                    ModifiedTime = f.ModifiedTime ?? DateTime.UtcNow,
                    MimeType = f.MimeType
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"List files failed: {ex.Message}");
            throw;
        }
    }
    
    // Implement remaining ICloudProvider methods...
}
```

**Acceptance Criteria:**
- [ ] Google API NuGet packages installed
- [ ] AuthenticateAsync() shows OAuth dialog
- [ ] Files listed successfully
- [ ] Build succeeds

---

## 🔑 Secure Credential Storage

### Step 1: Store Credentials Securely

**DO NOT hardcode** secrets in source code!

**Option A: User Secrets (Recommended for Development)**

```bash
# Navigate to project
cd src/FocusDeck.Core

# Store OAuth credentials
dotnet user-secrets init
dotnet user-secrets set "OAuth:OneDrive:ClientId" "1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p"
dotnet user-secrets set "OAuth:OneDrive:ClientSecret" "Abc~D.EFGhIjkL-mnopQRst_UVWxyz123456"
dotnet user-secrets set "OAuth:Google:ClientId" "123456789-abc.apps.googleusercontent.com"
dotnet user-secrets set "OAuth:Google:ClientSecret" "GOCSPX-AbCdEfGhIjKlMnOpQrStUvWxYz"

# List all secrets
dotnet user-secrets list
```

**Step 2: Read from User Secrets in Code**

```csharp
using Microsoft.Extensions.Configuration;

public class OneDriveProvider : ICloudProvider
{
    private readonly IConfiguration _config;
    
    private string CLIENT_ID => _config["OAuth:OneDrive:ClientId"] ?? throw new InvalidOperationException("ClientId not configured");
    private string CLIENT_SECRET => _config["OAuth:OneDrive:ClientSecret"] ?? throw new InvalidOperationException("ClientSecret not configured");
    
    public OneDriveProvider(IConfiguration config)
    {
        _config = config;
    }
}
```

**Option B: Azure Key Vault (Recommended for Production)**

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var keyVaultUrl = new Uri("https://focusdeck-keyvault.vault.azure.net/");
var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

var clientIdSecret = await client.GetSecretAsync("OneDriveClientId");
var clientSecretSecret = await client.GetSecretAsync("OneDriveClientSecret");

string CLIENT_ID = clientIdSecret.Value.Value;
string CLIENT_SECRET = clientSecretSecret.Value.Value;
```

---

## 🧪 Integration Testing Checklist

### Test 1: OneDrive Authentication
```csharp
[TestMethod]
public async Task OneDriveProvider_Authenticate_ShowsOAuthDialog()
{
    var config = new ConfigurationBuilder()
        .AddUserSecrets("focusdeck-secrets")
        .Build();
    
    var provider = new OneDriveProvider(config);
    var result = await provider.AuthenticateAsync();
    
    Assert.IsTrue(result);
}
```

**Run:**
```bash
dotnet test src/FocusDeck.Core --filter "OneDrive"
```

### Test 2: File Operations
```csharp
[TestMethod]
public async Task OneDriveProvider_ListFiles_ReturnsFiles()
{
    var provider = new OneDriveProvider(config);
    await provider.AuthenticateAsync();
    
    var files = await provider.ListFilesAsync("/");
    
    Assert.IsNotNull(files);
    Assert.IsTrue(files.Length > 0);
}
```

### Test 3: Google Drive Authentication
```csharp
[TestMethod]
public async Task GoogleDriveProvider_Authenticate_ShowsOAuthDialog()
{
    var config = new ConfigurationBuilder()
        .AddUserSecrets("focusdeck-secrets")
        .Build();
    
    var provider = new GoogleDriveProvider(config);
    var result = await provider.AuthenticateAsync();
    
    Assert.IsTrue(result);
}
```

---

## 📝 Implementation Order

### Phase 6b Week 1: Foundation
- [ ] MAUI project created
- [ ] DI setup complete
- [ ] Basic pages and navigation working
- **API integration deferred**

### Phase 6b Week 2: Timer Page
- [ ] Study timer UI working
- [ ] Data binding complete
- **API integration deferred**

### Phase 6b Week 3: Database & Sync Prep
- [ ] SQLite database setup
- [ ] Data models ready
- [ ] **START API Integration here**

### Phase 6b Week 4: Cloud Sync Integration
- [ ] OAuth2 flows implemented
- [ ] File upload/download working
- [ ] Encryption/decryption tested

### Phase 6b Week 5: Release
- [ ] API integration complete and tested
- [ ] All cloud features working
- [ ] Ready for TestFlight/Play Store

---

## 🎯 Success Criteria

### Minimum Viable Setup
```
✅ OneDrive Provider
  ├─ OAuth2 authentication working
  ├─ Can list files from cloud
  └─ File hash/timestamps working

✅ Google Drive Provider (optional)
  ├─ OAuth2 authentication working
  ├─ Can list files from cloud
  └─ Folder structure working
```

### Production Ready
```
✅ Both providers fully implemented
✅ Refresh token flow working
✅ Error handling robust
✅ User-friendly error messages
✅ Offline graceful degradation
✅ All tests passing
```

---

## 🚨 Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| OAuth dialog doesn't appear | MSAL/Google not installed | Run `dotnet add package` commands |
| "Invalid redirect URI" | Mismatch with registered | Update code or registration |
| Token expires silently | No refresh token | Ensure `offline_access` scope |
| Can't access secrets | User secrets not init | Run `dotnet user-secrets init` |
| Build errors | Missing using statements | Add `using Microsoft.Identity.Client;` |

---

## 🔗 Resources

- **API Setup:** `./API_SETUP_GUIDE.md`
- **OAuth2 Details:** `./docs/OAUTH2_SETUP.md` (coming soon)
- **Cloud Sync Design:** `./docs/CLOUD_SYNC_ARCHITECTURE.md`
- **Phase 6b Plan:** `./docs/PHASE6b_IMPLEMENTATION.md`

---

## ✅ Checklist: Ready for Phase 6b?

Before starting Phase 6b MAUI development:

```
API Setup:
  [ ] Microsoft OneDrive credentials obtained
  [ ] Google Drive credentials obtained (optional)
  [ ] API_SETUP_GUIDE.md read and understood

Preparation:
  [ ] MAUI project will be created (Week 1)
  [ ] OAuth2 packages added (Week 3)
  [ ] Credentials stored securely (Week 3)

Understanding:
  [ ] OAuth2 flow understood (above)
  [ ] Credential storage understood (above)
  [ ] Integration timeline understood (Week 3-4)

Ready:
  [ ] Can start Phase 6b Week 1
  [ ] Don't need to wait for API implementation
  [ ] API integration done in Week 3-4
```

---

**Next Step:** Begin Phase 6b - `docs/PHASE6b_IMPLEMENTATION.md` Week 1
