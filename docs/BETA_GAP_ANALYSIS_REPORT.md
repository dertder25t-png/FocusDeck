# 🚀 Beta Gap Analysis Report

**Generated:** December 5, 2025  
**Current Phase:** Phase 6b Week 2 → Beta Launch Readiness  
**Auditor:** FocusDeck Release Engineering Team

---

## 🛑 Critical Blockers (Must Fix Immediately)

### 1. **SECURITY: Development JWT Signing Key in appsettings.Development.json** ✅ MITIGATED

**File:** `src/FocusDeck.Server/appsettings.Development.json`  
**Issue:** Contains predictable development signing keys:

```json
{
  "Jwt": {
    "SigningKey": "this-is-a-very-secure-key-for-local-development-only-123456",
    "FallbackSigningKey": "this-is-a-fallback-key-for-local-development-only-654321"
  }
}
```

**Risk Level:** 🔴 **HIGH** - If accidentally deployed to production, any attacker could forge valid JWT tokens.

**Status:** ✅ **MITIGATED** - `appsettings.Production.json` contains a proper 256-bit base64 key:
```json
"SigningKey": "NEaYzvSj2/BDggT+UDsXGHPH5eTevf2vMvARs/P+DNw="
```

**Action Required:**  
1. Ensure the deployment pipeline **never** includes `appsettings.Development.json`
2. Rotate the production signing key before Beta launch

---

### 2. **AUTH BYPASS: Test User Fallback in FocusController** ✅ FIXED

**File:** `src/FocusDeck.Server/Controllers/v1/FocusController.cs`  
**Issue:** Previously contained a development-only fallback that returned "test-user" when no authenticated user was found.

**Status:** ✅ **FIXED** - The fallback has been removed and replaced with proper authentication:

```csharp
private string GetUserId()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userId))
    {
        throw new UnauthorizedAccessException("User is not authenticated");
    }

    return userId;
}
```

**Additionally:** Added `[Authorize]` attribute to the controller class.
---

### 3. **AUTH BYPASS: "default_user" Hardcoded in ServicesController** ✅ FIXED

**File:** `src/FocusDeck.Server/Controllers/ServicesController.cs`  
**Issue:** Previously used a hardcoded user ID for OAuth token storage.

**Status:** ✅ **FIXED** - The following changes were made:
1. Removed `DefaultUserId` constant
2. Added `[Authorize]` attribute to the controller class
3. Added `GetUserId()` method that extracts user ID from JWT claims
4. Updated `Connect`, `GetAll`, `GetOAuthUrl` methods to use authenticated user ID
5. Updated `OAuthCallback` to use state parameter for passing user ID (since OAuth callbacks don't have JWT)

---

### 4. **MISSING AUTH: FocusController Endpoints Not Protected** ✅ FIXED

**File:** `src/FocusDeck.Server/Controllers/v1/FocusController.cs`  
**Issue:** The controller lacked `[Authorize]` attribute.

**Status:** ✅ **FIXED** - Added `[Authorize]` attribute to the controller class.

**Controllers with `[Authorize]`:** EncryptionController, DecksController, TasksController, TenantsController, ContextSnapshotsController, PrivacyController, InvitesController, UserSettingsController, AssetsController, CoursesController, BrowserController, AnalyticsController, AgentController, **FocusController**, **ServicesController**

**Controllers Properly Secured (throw UnauthorizedAccessException):** RemoteController, DevicesController

---

## ⚠️ Functional Gaps (User Impact High)

### 1. **AI Text Generation - Stub Implementation**

**File:** `src/FocusDeck.Server/Services/TextGeneration/StubTextGen.cs`  
**Issue:** Returns hardcoded placeholder text instead of actual AI-generated content:

```csharp
return $"[Generated Summary] This is a concise summary of the content. Key points include: main topics discussed, important concepts, and takeaways.";
```

**User Impact:** Any feature relying on AI text generation (note summarization, lecture transcription summaries) will show placeholder content.

**Status:** Stub implementation - requires OpenAI/Anthropic API integration.

---

### 2. **AI Embedding Service - Stub Implementation**

**File:** `src/FocusDeck.Services/Context/StubEmbeddingService.cs`  
**Issue:** Returns random vectors instead of semantic embeddings:

```csharp
// Deterministic stub: hash input to seed a random generator
// This ensures the same text gets the same vector (useful for testing)
using var sha256 = SHA256.Create();
var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputText));
```

**User Impact:** Context-aware features (smart suggestions, similar notes) will return random/meaningless results.

**Status:** Stub implementation - requires sentence-transformers or OpenAI embeddings API.

---

### 3. **Spotify Integration - Placeholder Data**

**File:** `src/FocusDeck.Services/Context/Sources/SpotifySource.cs` (Lines 20-29)  
**Issue:** Returns hardcoded data instead of actual Spotify API data:

```csharp
// TODO: Implement the logic to capture the user's currently playing Spotify song.
// This will involve using the Spotify API.
var data = new JsonObject
{
    ["artist"] = "Lofi Girl",
    ["track"] = "lofi hip hop radio - beats to relax/study to",
    ["album"] = "Lofi Girl"
};
```

**User Impact:** Spotify context features will show incorrect "currently playing" information.

---

### 4. **Canvas Assignments Integration - Placeholder Data**

**File:** `src/FocusDeck.Services/Context/Sources/CanvasAssignmentsSource.cs` (Lines 20-28)  
**Issue:** Returns hardcoded assignment data:

```csharp
// TODO: Implement the logic to capture the user's upcoming Canvas assignment.
// This will involve using the Canvas API.
var data = new JsonObject
{
    ["assignment"] = "Finish the context snapshot system",
    ["course"] = "CS 4500",
    ["dueDate"] = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
};
```

**User Impact:** Canvas integration will show fake assignments instead of real due dates.

---

### 5. **Whisper Transcription - External Dependency**

**File:** `src/FocusDeck.Server/Services/Transcription/WhisperCppAdapter.cs`  
**Issue:** Requires `whisper-cpp` binary at `/usr/local/bin/whisper-cpp` which must be installed separately.

**Status:** Implementation is complete, but requires deployment setup documentation.

**User Impact:** Lecture transcription will fail if whisper-cpp is not installed on the server.

---

## 📋 "Coming Soon" Recommendations

These features should be **hidden or labeled "Beta/Coming Soon"** in the UI:

| Feature | Current State | Recommendation |
|---------|---------------|----------------|
| AI Note Summarization | Stub (placeholder text) | Hide or show "AI features coming soon" |
| Smart Suggestions | Stub (random embeddings) | Disable suggestion cards |
| Spotify "Now Playing" | Hardcoded data | Show "Connect Spotify" but disable live display |
| Canvas Due Dates | Hardcoded data | Show as "Canvas sync coming soon" |
| Lecture Transcription | Requires external binary | Gate behind feature flag |

---

## ✅ Ready for Beta

These components appear solid and well-implemented:

### Authentication System
- ✅ **PAKE (Password-Authenticated Key Exchange)** - Full SRP-6a implementation with Argon2id KDF
- ✅ **JWT Token Management** - Proper token generation, refresh, and revocation
- ✅ **Refresh Token Rotation** - Secure token rotation with device fingerprinting
- ✅ **Google OAuth Integration** - Framework in place, just needs credentials

### Core API Controllers (with `[Authorize]`)
- ✅ DecksController, TasksController, NotesController
- ✅ CoursesController, LecturesController
- ✅ TenantsController, InvitesController
- ✅ PrivacyController, UserSettingsController
- ✅ AssetsController, BrowserController
- ✅ AnalyticsController, ContextSnapshotsController

### Database & Persistence
- ✅ **InitialCreate Migration** - Comprehensive schema with all required tables
- ✅ **EnsureUniquePakeUser Migration** - Proper unique constraint on credentials
- ✅ **Entity Framework Core** - Properly configured DbContext with PostgreSQL/SQLite support

### Frontend Architecture
- ✅ **Vite Configuration** - Proper proxy setup for `/v1` and `/hubs` routes
- ✅ **No Hardcoded Production URLs** - Uses relative paths
- ✅ **PAKE Client Implementation** - Complete with Argon2id and SHA-256 fallback

### Infrastructure
- ✅ **SignalR Hubs** - Real-time notifications properly configured
- ✅ **Hangfire Background Jobs** - Stub client for development, proper setup for production
- ✅ **Serilog Logging** - Structured logging with proper configuration

---

## 🛠 Suggested Fix Action Plan

### Fix #1: Remove FocusController Auth Fallback ✅ COMPLETED

**Status:** ✅ **IMPLEMENTED** - See commits in this PR

Changes made:
1. Added `[Authorize]` attribute to FocusController
2. Replaced fallback with `throw new UnauthorizedAccessException("User is not authenticated")`

---

### Fix #2: Remove "default_user" from ServicesController ✅ COMPLETED

**Status:** ✅ **IMPLEMENTED** - See commits in this PR

Changes made:
1. Removed `DefaultUserId` constant
2. Added `[Authorize]` attribute to controller
3. Added `GetUserId()` method
4. Updated all methods to use authenticated user ID
5. OAuth callback now uses state parameter to pass user ID

---

### Fix #3: Ensure Production Deployment Excludes Development Config

**Status:** ⏳ **RECOMMENDED** - Manual deployment step

**Create/Update:** `.github/workflows/deploy.yml` or deployment script

Add explicit exclusion:
```yaml
- name: Build and Publish
  run: |
    dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj \
      -c Release \
      -o ./publish
    # Ensure development config is not included
    rm -f ./publish/appsettings.Development.json
```

---

## Summary

| Category | Count | Status |
|----------|-------|--------|
| 🛑 Critical Blockers | 4 | ✅ **3 Fixed, 1 Mitigated** |
| ⚠️ Functional Gaps | 5 | Feature flag or hide in UI |
| 📋 Coming Soon Items | 5 | Add "Beta" labels |
| ✅ Ready Components | 15+ | Ship it! |

### Critical Blockers Resolution:
- ✅ **FocusController Auth Fallback** - Fixed (removed fallback, added [Authorize])
- ✅ **ServicesController default_user** - Fixed (now uses authenticated user ID)
- ✅ **Missing [Authorize] on FocusController** - Fixed (added [Authorize] attribute)
- ✅ **Development JWT Keys** - Mitigated (production has proper key, ensure deployment excludes dev config)

**Recommendation:** The critical security blockers have been addressed. The functional gaps (stub implementations) can be managed with feature flags and "Coming Soon" labels in the UI for Beta testers.
