# Alternative Testing Methods - Without .NET SDK

## ⚠️ SCENARIO: No .NET SDK Available

If your developer environment doesn't have access to the .NET SDK, here are **6 alternative approaches** to test authentication fixes and validate code.

---

## 🔷 OPTION 1: REMOTE TESTING (Recommended)

### Use a test server that HAS .NET installed

Your developer can:
1. Push code changes to a branch
2. Run tests on a server that has .NET (CI/CD pipeline, build server, or deployment environment)
3. Get test results remotely

**Steps:**
```bash
# Developer pushes changes
cd /root/FocusDeck
git add .
git commit -m "WIP: Fix authentication tests"
git push origin branch-name

# Build server / CI pipeline automatically runs:
# dotnet build
# dotnet test

# Developer sees results in CI/CD dashboard
```

**Platforms to use:**
- GitHub Actions (free)
- GitLab CI
- Azure Pipelines
- Jenkins
- Any existing CI/CD system

---

## 🐳 OPTION 2: DOCKER (If Available)

### Use Docker image with .NET pre-installed

If Docker is available in the environment:

```bash
# 1. Run a .NET container
docker run -it --rm \
  -v /root/FocusDeck:/workspace \
  -w /workspace \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  /bin/bash

# 2. Inside container, run tests
dotnet test

# 3. Exit when done
exit
```

**Dockerfile to create custom image:**

Create `/root/FocusDeck/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app

COPY . .

RUN dotnet restore
RUN dotnet build

CMD ["dotnet", "test"]
```

**Run it:**
```bash
docker build -t focusdeck-tests .
docker run --rm focusdeck-tests
```

---

## 🔍 OPTION 3: CODE REVIEW & MANUAL VERIFICATION

### Verify the fix by code inspection (No execution needed)

Your developer can manually verify the authentication fix by analyzing the code:

**What to check in FocusDeckWebApplicationFactory.cs:**

```csharp
// ✅ SHOULD HAVE: JWT token generation
public string GenerateTestJwtToken(string userId)
{
    // Create JWT with claims, signature, expiration
    // Return valid token string
}

// ✅ SHOULD HAVE: Authenticated HTTP client
public HttpClient CreateAuthenticatedClient(string userId)
{
    var client = Server.CreateClient();
    var token = GenerateTestJwtToken(userId);
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    return client;
}

// ✅ SHOULD HAVE: Constructor that doesn't need auth
public HttpClient CreateUnauthenticatedClient()
{
    return Server.CreateClient();
}
```

**What to check in AutomationWorkflowIntegrationTests.cs:**

```csharp
// ❌ WRONG - This gets 401 Unauthorized
var client = _factory.CreateClient();
var response = await client.PostAsync("/api/AutomationWorkflows", ...);

// ✅ CORRECT - Uses authenticated client
var client = _factory.CreateAuthenticatedClient("test-user@example.com");
var response = await client.PostAsync("/api/AutomationWorkflows", ...);
```

---

## 📝 OPTION 4: STATIC CODE ANALYSIS

### Use code analysis tools to verify the fix

**Python-based analysis:**
```bash
# Search for the authentication fix patterns
grep -r "GenerateTestJwtToken" /root/FocusDeck/tests/
grep -r "CreateAuthenticatedClient" /root/FocusDeck/tests/
grep -r "\[Authorize\]" /root/FocusDeck/src/FocusDeck.Server/Controllers/
```

**Check for required changes:**
```bash
# Should find these patterns in the fixed code
grep "Authorization.*Bearer" /root/FocusDeck/tests/FocusDeck.Server.Tests/FocusDeckWebApplicationFactory.cs
grep "\.AddJwt" /root/FocusDeck/tests/FocusDeck.Server.Tests/FocusDeckWebApplicationFactory.cs
```

**JavaScript/Node-based analysis (if available):**
```bash
npm install -g eslint
# Or use other static analysis tools
```

---

## 🌐 OPTION 5: HTTP TESTING (Curl + Scripts)

### Test authentication against a running server

**If a development server is running:**

```bash
# 1. Start the server (in another terminal/environment)
dotnet run --project src/FocusDeck.Server

# 2. Test without authentication (should get 401)
curl -X POST http://localhost:5000/api/AutomationWorkflows \
  -H "Content-Type: application/json" \
  -d '{"name":"test"}' \
  -i

# Expected: HTTP 401 Unauthorized

# 3. Test with authentication (should get 201 or 400 validation error, not 401)
TOKEN="your-jwt-token-here"
curl -X POST http://localhost:5000/api/AutomationWorkflows \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"test"}' \
  -i

# Expected: HTTP 201 Created or 400 Bad Request (not 401!)
```

**Create a test script:**

```bash
#!/bin/bash
# test-auth.sh

SERVER="http://localhost:5000"

# Get JWT token
echo "Testing authentication endpoints..."

# Test 1: Without token (should fail)
echo "Test 1: Request without authentication"
curl -X POST $SERVER/api/AutomationWorkflows \
  -H "Content-Type: application/json" \
  -d '{"name":"test"}' \
  --write-out "\nStatus: %{http_code}\n"

# Test 2: With token (should succeed or give different error)
echo "Test 2: Request with authentication"
curl -X POST $SERVER/api/AutomationWorkflows \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer fake-token" \
  -d '{"name":"test"}' \
  --write-out "\nStatus: %{http_code}\n"

# Test 3: Check if [Authorize] is working
echo "Test 3: List workflows without auth (should be 401)"
curl -X GET $SERVER/api/AutomationWorkflows \
  --write-out "\nStatus: %{http_code}\n"

echo "Test 4: List workflows with auth (should be 200 or 401 for invalid token)"
curl -X GET $SERVER/api/AutomationWorkflows \
  -H "Authorization: Bearer fake-token" \
  --write-out "\nStatus: %{http_code}\n"

echo "Authentication tests complete!"
```

**Run it:**
```bash
chmod +x test-auth.sh
./test-auth.sh
```

---

## 🔧 OPTION 6: INSTALL DOTNET IN DEVELOPER'S ENVIRONMENT

### As a last resort - Install .NET in the sandbox

**For Linux (Debian/Ubuntu):**

```bash
# 1. Download .NET installer
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh

# 2. Install to user directory (no sudo needed)
./dotnet-install.sh --channel 9.0

# 3. Add to PATH
export PATH="$HOME/.dotnet:$PATH"

# 4. Verify
dotnet --version

# 5. Now can run tests
dotnet test
```

**For Windows (PowerShell):**

```powershell
# Download installer
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"

# Run installer
.\dotnet-install.ps1 -Channel 9.0

# Add to PATH and verify
dotnet --version

# Run tests
dotnet test
```

---

## 📊 COMPARISON: WHICH METHOD TO USE?

| Method | Pros | Cons | Best For |
|--------|------|------|----------|
| **Remote Testing** | ✅ Automatic, Reliable | Requires push + wait | CI/CD pipelines |
| **Docker** | ✅ Complete isolation | Requires Docker | Full environment |
| **Code Review** | ✅ Fast, no tools needed | Manual, error-prone | Quick verification |
| **Static Analysis** | ✅ No execution needed | Limited validation | Code patterns |
| **HTTP Testing** | ✅ Tests real behavior | Requires running server | Live testing |
| **Install .NET** | ✅ Full functionality | Requires installation | Long-term solution |

---

## 🎯 RECOMMENDED APPROACH

**If developer CANNOT run dotnet:**

### **Quick Path (This Week):**
1. ✅ Developer works on code in IDE (no execution needed)
2. ✅ Push to branch
3. ✅ GitHub Actions / CI runs tests automatically
4. ✅ Developer sees results in GitHub
5. ✅ Make fixes based on CI results

### **Long-term Path (Best):**
1. Install .NET SDK in the environment
2. Or use Docker for .NET
3. Run tests locally for immediate feedback

---

## 🚀 IMMEDIATE ACTION: SET UP CI/CD TESTING

### GitHub Actions (Free & Already Available)

**Create `.github/workflows/test.yml`:**

```yaml
name: Run Tests

on:
  push:
    branches: [ phase-1 ]
  pull_request:
    branches: [ phase-1 ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Run tests
      run: dotnet test --logger "console;verbosity=detailed"
```

**How it works:**
1. Developer pushes code
2. GitHub Actions automatically runs the workflow
3. Tests run on GitHub's servers
4. Developer sees results in GitHub

---

## 📋 QUICK CHECKLIST: Testing Without Local dotnet

- [ ] Option 1: Push to branch and use CI/CD
- [ ] Option 2: If Docker available, use `docker run`
- [ ] Option 3: Manual code review of fixes
- [ ] Option 4: Use grep/analysis tools to verify patterns
- [ ] Option 5: Test against running server with curl
- [ ] Option 6: Install .NET in environment

---

## ❓ WHAT TO TELL YOUR DEVELOPER

**If they can't run dotnet locally:**

> "No problem! Here are your options:
> 
> 1. **Fastest**: Push your code to the `phase-1` branch. I've set up GitHub Actions to run tests automatically. Check the GitHub UI for results.
> 
> 2. **Offline**: Review your changes manually against the checklist below. Ensure you:
>    - Added JWT token generation to test factory
>    - Made HTTP client include Authorization header
>    - Updated tests to use authenticated client
>    - Added [Authorize] attributes to controllers
> 
> 3. **Test locally**: If you have Docker, run tests in a container
> 
> 4. **Test live server**: If you can access a running server, use curl to test endpoints"

---

## 🔗 RESOURCES

- **Install .NET**: https://dotnet.microsoft.com/download
- **GitHub Actions**: https://github.com/features/actions
- **Docker .NET Images**: https://hub.docker.com/_/microsoft-dotnet-sdk
- **CI/CD Services**: GitHub Actions, GitLab CI, Azure Pipelines, Jenkins

---

**Your developer has options! Pick the one that works best for their environment.**
