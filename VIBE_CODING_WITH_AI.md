# 🤖 VIBE CODING WITH AI + BMAD

**What is this?**  
Using AI (Copilot, Claude, Cursor, etc.) to write code *while* BMAD automatically handles testing, formatting, and deployment. You and AI flow together, BMAD keeps everything clean.

---

## 🎯 The AI + BMAD Flow

### **Phase 1: Dream with AI (5 minutes)**

You describe what you want to build. AI helps you plan it.

```
YOU: "I want to add a Spotify OAuth endpoint. Walk me through it."

AI: "Sure! Here's what we need:
     1. OAuth endpoint: GET /api/oauth/spotify/callback
     2. Service to handle token exchange
     3. Store refresh token in database
     4. Return user profile
     
     Want me to code it?"

YOU: "Yes, but make it production-grade with error handling"
```

---

### **Phase 2: Code with AI (20-40 minutes)**

You and AI write the feature together. AI writes code, you review and guide.

```
YOU: "Create the SpotifyService class with OAuth token exchange"

AI: Generates:
    ```csharp
    public class SpotifyService : ISpotifyService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenCache _cache;
        
        public async Task<AccessTokenDto> ExchangeCodeForTokenAsync(string code)
        {
            var request = new SpotifyTokenRequest
            {
                Code = code,
                GrantType = "authorization_code",
                RedirectUri = _options.RedirectUri
            };
            
            var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", ...);
            return await response.Content.ReadAsAsync<AccessTokenDto>();
        }
    }
    ```

YOU: "Good! Now add retry logic and better error handling"

AI: Updates with try-catch, retry policy, logging
```

**No stress about formatting.** BMAD will handle it.

---

### **Phase 3: BMAD Validates (1 minute)**

Run BMAD to catch any issues AI might have missed.

```powershell
.\bmad.ps1 run
```

**Output:**
```
Building FocusDeck...
✓ Build succeeded
✓ All tests passed
✓ Code formatted
✓ Security check: OK
✓ Coverage: 72%
```

**If something breaks:**
```
YOU: "Build failed: SpotifyService missing interface method"

AI: "Ah! We need to implement this method:
     public async Task<UserProfileDto> GetUserProfileAsync(string accessToken)
     
     Here's the implementation..."

YOU: "Add it"

AI: Adds the code

.\bmad.ps1 run    # ✓ Success!
```

---

### **Phase 4: AI Improves (Optional)**

Ask AI to improve what you built.

```
YOU: "Can you optimize the token caching? We're hitting Spotify API too much"

AI: "Sure! Let's implement a smarter cache:
     - Cache tokens with TTL
     - Refresh before expiry
     - Handle concurrent requests
     
     Here's the updated code..."

.\bmad.ps1 run    # ✓ Still works!
```

---

### **Phase 5: Ship (30 seconds)**

```powershell
git add -A
git commit -m "Add Spotify OAuth integration"
git push
```

GitHub Actions runs BMAD automatically.
If it passes → Live in production.

---

## 🎸 Real Example: Add Google Calendar API

### Step 1: Dream with AI

```
YOU: "I want to add Google Calendar integration. Start fresh - what do we need?"

CLAUDE/COPILOT:
"Here's the architecture:
 1. OAuth callback endpoint
 2. GoogleCalendarService to fetch events
 3. Store OAuth tokens in ConnectedService entity
 4. SignalR notification when event is fetched
 5. Unit tests for token refresh
 
 Ready to code?"
```

### Step 2: Code with AI

```
YOU: "Build GoogleCalendarService first. Make it production-grade."

AI GENERATES:
```csharp
public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly GoogleCalendarApiClient _apiClient;
    private readonly AutomationDbContext _db;
    private readonly ILogger<GoogleCalendarService> _logger;
    
    public async Task<List<CalendarEventDto>> GetUpcomingEventsAsync(
        string userId, 
        int maxResults = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching calendar events for user {UserId}", userId);
            
            var events = await _apiClient.GetEventsAsync(
                new GetEventsRequest
                {
                    CalendarId = "primary",
                    TimeMin = DateTime.UtcNow,
                    TimeMax = DateTime.UtcNow.AddDays(7),
                    MaxResults = maxResults,
                    OrderBy = "startTime",
                    SingleEvents = true
                },
                ct);
            
            _logger.LogInformation("Retrieved {Count} events", events.Count);
            return events.Select(e => e.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching calendar events");
            throw;
        }
    }
}
```

YOU: "Perfect! Now add the OAuth callback handler"

AI GENERATES CONTROLLER:
```csharp
[HttpGet("oauth/google-calendar/callback")]
public async Task<IActionResult> GoogleCalendarCallback(
    [FromQuery] string code,
    CancellationToken ct)
{
    var tokenResponse = await _googleService.ExchangeCodeForTokenAsync(code, ct);
    
    var connectedService = new ConnectedService
    {
        ServiceType = ServiceType.GoogleCalendar,
        AccessToken = tokenResponse.AccessToken,
        RefreshToken = tokenResponse.RefreshToken,
        TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
        IsConfigured = true
    };
    
    _db.ConnectedServices.Add(connectedService);
    await _db.SaveChangesAsync(ct);
    
    return Ok(new { message = "Google Calendar connected!" });
}
```

YOU: "Add tests for token refresh"

AI ADDS TESTS:
```csharp
[Fact]
public async Task ExchangeCodeForToken_WithValidCode_ReturnsValidToken()
{
    // Arrange
    var code = "valid_auth_code";
    var expectedToken = "access_token_value";
    
    _httpClientMock.Setup(...)
        .ReturnsAsync(new HttpResponseMessage { Content = ... });
    
    // Act
    var result = await _service.ExchangeCodeForTokenAsync(code);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedToken, result.AccessToken);
}
```
```

### Step 3: BMAD Validates

```powershell
.\bmad.ps1 run
```

Output:
```
Building FocusDeck...
✓ Build succeeded (45 seconds)
✓ Tests: 156 passed (2 minutes)
✓ Code formatted (5 files updated)
✓ Security check: OK
✓ Performance: P95 response time 245ms
```

### Step 4: AI Improves

```
YOU: "Response time is good but let's cache the calendar events. Users check often."

AI: "Great idea! Let's add Redis caching:
     - Cache events for 15 minutes
     - Invalidate on new event creation
     - Background job to refresh weekly
     
     Here's the updated code..."

AI UPDATES SERVICE:
```csharp
public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IDistributedCache _cache;
    
    public async Task<List<CalendarEventDto>> GetUpcomingEventsAsync(
        string userId, 
        int maxResults = 10,
        CancellationToken ct = default)
    {
        var cacheKey = $"calendar_events:{userId}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<CalendarEventDto>>(cached)!;
        }
        
        // ... fetch from API ...
        
        await _cache.SetStringAsync(cacheKey, jsonResult, 
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) 
            }, 
            ct);
        
        return events;
    }
}
```

.\bmad.ps1 run    # ✓ Still works!
```

### Step 5: Ship

```powershell
git add -A
git commit -m "Add Google Calendar integration with caching"
git push origin feature/google-calendar
```

---

## 🔄 The AI + BMAD Workflow in Action

```
YOU                          AI                              BMAD
|                            |                               |
|--- "Build OAuth flow" ---->|                               |
|                            |--- Generates code --->        |
|                            |                               |
|<--- "Review this" ---------|                               |
|                            |                               |
|--- "Add caching" --------->|                               |
|                            |--- Updates code --->          |
|                            |                               |
|--- Run .\bmad.ps1 run --------------------->|              |
|                                             |--- Builds    |
|                                             |--- Tests     |
|                                             |--- Formats   |
|                                             |--- Reports   |
|<-------- "All good!" ----------------------|              |
|                            |                               |
|--- Push to GitHub -------->| (automatic deploy)            |
|                            |                               |
|--- Feature live! <---------|<------- CI/CD handles         |
```

---

## 💻 Using AI Tools with BMAD

### **GitHub Copilot (VS Code)**

```csharp
// Start typing, Copilot suggests:

public class SpotifyService : ISpotifyService
{
    // Copilot fills in the rest
    public async Task<AccessTokenDto> ExchangeCodeAsync(string code)
    {
        // Copilot generates:
        var httpClient = new HttpClient();
        var request = new { code, client_id = ... };
        // ... etc
    }
}
```

Then run:
```powershell
.\bmad.ps1 run    # BMAD validates what Copilot wrote
```

### **Claude (Conversation)**

```
YOU: "Build a complete OAuth service for Spotify. Include token refresh,
     error handling, tests, and logging."

CLAUDE: (Generates entire service with all requirements)

YOU: Copy the code into VS Code

.\bmad.ps1 run    # BMAD tests it all
```

### **Cursor (AI IDE)**

```
1. Open Cursor
2. Use CMD+K to ask AI to write code
3. AI generates feature
4. Accept/Modify
5. .\bmad.ps1 run
```

---

## 🎯 Pro Tips for AI + BMAD Vibe Coding

### **1. Ask AI for Complete Features**

```
GOOD:
"Build the complete OAuth flow for Spotify:
 - Endpoint to get auth URL
 - Callback handler
 - Token storage
 - Token refresh service
 - Error handling
 - Unit tests
 - Logging"

BAD:
"Write a function to get a token"
```

### **2. Let AI Write Tests**

```
YOU: "Now write comprehensive unit tests for SpotifyService"

AI: (Generates 10+ tests with mocks and assertions)

YOU: Review them

.\bmad.ps1 run    # Verify they all pass
```

### **3. Ask AI to Explain Before Coding**

```
YOU: "Before coding, explain how OAuth 2.0 works and how we'll implement it"

AI: (Detailed explanation)

YOU: "Now write the implementation"

AI: (Better implementation because you both understand the architecture)
```

### **4. Use BMAD to Catch AI Mistakes**

```
AI might miss:
- Error handling
- Null checks
- Missing dependencies
- Performance issues

But BMAD will catch them:
.\bmad.ps1 run    # Tests fail, you see the issue

YOU: "Fix this: [error message]"

AI: "Got it, here's the fix"

.\bmad.ps1 run    # Works!
```

---

## 🌊 Your Daily AI + BMAD Flow

### Morning

```powershell
# 1. Create feature
git checkout -b feature/add-canvas-api

# 2. Talk to AI
# "Build Canvas API integration service with token refresh"

# 3. AI generates code, you paste it in

# 4. BMAD validates
.\bmad.ps1 run

# 5. Ship
git push
```

### Afternoon

```powershell
# 1. Create feature
git checkout -b feature/optimize-queries

# 2. Talk to AI
# "Profile these queries and optimize them with better indexes"

# 3. AI suggests optimizations, you apply them

# 4. BMAD validates performance
.\bmad.ps1 run

# 5. Ship
git push
```

### Evening

```powershell
# 1. Create feature
git checkout -b bugfix/fix-remote-control-lag

# 2. Talk to AI
# "Debug why remote control has 2-second lag. Suggest fixes."

# 3. AI analyzes code, suggests fixes

# 4. BMAD validates the fix works
.\bmad.ps1 run

# 5. Ship
git push
```

---

## 🚀 The Magic Combo

| Alone | With BMAD | With AI | With AI + BMAD |
|------|-----------|---------|----------------|
| Manual coding | Automated testing | Fast coding | Fast, tested, perfect |
| Manual testing | Saves time | May have bugs | No bugs |
| Manual formatting | Saves time | Messy code | Always clean |
| Manual deployment | Saves time | Can't deploy safely | Safe deploy |
| Hours | Minutes | 30 min | 15 min |
| Stressful | Confident | "Did AI mess up?" | 100% confident |

**AI + BMAD = Maximum vibe.**

---

## 📋 Quick Checklist: AI + BMAD Session

- [ ] Create feature branch
- [ ] Ask AI to build complete feature
- [ ] Paste AI code into project
- [ ] Run `.\bmad.ps1 run`
  - [ ] Build succeeds
  - [ ] Tests pass
  - [ ] Code formatted
  - [ ] No security issues
- [ ] Ask AI to add improvements/features
- [ ] Run `.\bmad.ps1 run` again
- [ ] Push to GitHub
- [ ] GitHub Actions auto-deploys
- [ ] Feature is live ✨

---

## 🎵 The Vibe

**Old AI Coding:**
- Write with AI
- Manually test
- Hope it works
- Debug for hours
- Not vibing

**AI + BMAD:**
- Write with AI
- `.\bmad.ps1 run` (everything works)
- Push
- Live in production
- Always vibing 🌊

---

**Ready to vibe code with AI?**

1. Open your AI tool (Copilot, Claude, Cursor)
2. Ask it to build a feature
3. Paste the code
4. Run: `.\bmad.ps1 run`
5. Ship with confidence

**Go vibe. 🤖 + 🛠️ = ✨**
