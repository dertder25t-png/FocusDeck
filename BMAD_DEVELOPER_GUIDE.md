# 🏗️ BMAD-METHOD Developer Guide for FocusDeck

**Last Updated:** November 4, 2025  
**Status:** Phase 6b - Remote Control & OAuth Integration

---

## 🎯 Quick Start (5 minutes)

### First Time Setup

```bash
# 1. Clone FocusDeck repo
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck

# 2. Initialize BMAD (if submodule not yet cloned)
git submodule update --init --recursive

# 3. Restore .NET dependencies
dotnet restore

# 4. Run complete BMAD cycle
./tools/BMAD-METHOD/bmad run

# Result: Your code is built, tested, measured, and ready!
```

### Local Development Loop

```bash
# While actively developing:

# 1. Make your changes
# Edit: src/FocusDeck.Server/Controllers/MyController.cs

# 2. Build + test
./tools/BMAD-METHOD/bmad build && dotnet test

# 3. Measure & adapt
./tools/BMAD-METHOD/bmad measure && ./tools/BMAD-METHOD/bmad adapt

# 4. Push when ready
git add -A && git commit -m "Add feature" && git push
```

---

## 📋 BMAD-METHOD Phases Explained

### Phase 1: BUILD ⚙️

**What:** Compile your code, restore packages, link dependencies

**Command:**
```bash
./tools/BMAD-METHOD/bmad build
```

**What happens:**
- Runs `dotnet build` on all modules
- Validates project references
- Creates compiled output in `./bin`
- Generates publish artifacts in `./publish`

**Modules built (in order):**

| Module | Purpose | Dependencies |
|--------|---------|--------------|
| Domain | Entities (StudySession, RemoteAction) | None |
| Persistence | EF Core DbContext | Domain |
| Contracts | DTOs & Validators | None |
| Shared | Cross-platform models | Contracts |
| Server | REST API, SignalR | All above |
| Desktop | Windows WPF UI | Shared |
| Mobile | Android MAUI UI | Shared |

**Expected output:**
```
✅ FocusDeck.Domain:        Build succeeded
✅ FocusDeck.Persistence:   Build succeeded
✅ FocusDeck.Contracts:     Build succeeded
✅ FocusDeck.Shared:        Build succeeded
✅ FocusDeck.Server:        Build succeeded
✅ FocusDeck.Desktop:       Build succeeded (Windows only)
✅ FocusDeck.Mobile:        Build succeeded (requires MAUI workload)
```

---

### Phase 2: MEASURE 📊

**What:** Run tests, check health, collect performance data

**Command:**
```bash
./tools/BMAD-METHOD/bmad measure
```

**What happens:**
- Starts local server on http://localhost:5000
- Runs unit tests (dotnet test)
- Runs integration tests
- Checks API health endpoints
- Measures response times
- Collects telemetry logs
- Generates test coverage report

**Health checks performed:**

```
✅ Server health check
   GET http://localhost:5000/healthz
   Expected: 200 OK with {"ok":true}

✅ API services check
   GET http://localhost:5000/api/services
   Expected: 200 OK with list of services

✅ Remote control check
   GET http://localhost:5000/v1/remote/actions
   Expected: 200 OK
```

**Test results:**
```
Unit Tests:        523 passed, 0 failed
Integration Tests: 45 passed, 0 failed
Code Coverage:     78% (target: 70%)
```

**Performance targets:**
- API response P95: < 500ms (target)
- API response P99: < 1000ms (critical)
- Build time: < 60s (target)

---

### Phase 3: ADAPT 🔧

**What:** Format code, run analysis, check security

**Command:**
```bash
./tools/BMAD-METHOD/bmad adapt
```

**What happens:**
- Auto-formats C# code (`dotnet format`)
- Runs static analysis
- Checks for security vulnerabilities
- Reports outdated NuGet packages
- Identifies code quality issues

**Code formatting:**
```bash
# Auto-format all C# files
dotnet format src/

# Check format without changing
dotnet format --verify-no-changes
```

**Security scan:**
```bash
# Check for vulnerable NuGet packages
dotnet list package --vulnerable

# Example output:
⚠️  Package X has vulnerability CVE-XXXX (severity: high)
✅ All other packages are safe
```

**Static analysis:**
```bash
# Run build with warnings as errors
dotnet build /warnaserror-

# Fix warnings identified
```

---

### Phase 4: DEPLOY 🚀

**What:** Publish build artifacts, deploy to production server

**Command:**
```bash
./tools/BMAD-METHOD/bmad deploy
```

**Prerequisites:**
- GitHub secrets configured (DEPLOY_HOST, DEPLOY_USER, DEPLOY_KEY)
- SSH access to Linux server (192.168.1.110)
- systemd service configured (focusdeck)

**What happens:**

1. **Publish server build (Release mode)**
   ```bash
   dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj \
     -c Release \
     -o ./publish/server \
     --self-contained
   ```

2. **Create deployment artifact**
   ```bash
   tar -czf focusdeck-server.tar.gz ./publish/server
   ```

3. **SSH to Linux server**
   ```bash
   scp focusdeck-server.tar.gz focusdeck@192.168.1.110:/tmp/
   ```

4. **Stop systemd service**
   ```bash
   sudo systemctl stop focusdeck
   ```

5. **Extract & deploy binaries**
   ```bash
   sudo tar -xzf /tmp/focusdeck-server.tar.gz \
     -C /home/focusdeck/FocusDeck/src/FocusDeck.Server/bin/Release/net9.0/
   ```

6. **Restart systemd service**
   ```bash
   sudo systemctl restart focusdeck
   ```

7. **Verify health check**
   ```bash
   curl https://focusdeck.909436.xyz/healthz
   ```

8. **Rollback if failed** (automatically)
   ```bash
   sudo systemctl restart focusdeck  # Revert to previous version
   ```

**Expected output:**
```
🔨 Publishing server build...
✅ Published to ./publish/server

📦 Creating deployment artifact...
✅ Created focusdeck-server.tar.gz

🔐 Connecting to server...
✅ Connected to 192.168.1.110

⏸️  Stopping systemd service...
✅ Service stopped

📂 Deploying binaries...
✅ Deployed successfully

▶️  Starting systemd service...
✅ Service started

🏥 Verifying health check...
✅ Server healthy at https://focusdeck.909436.xyz/healthz

🎉 Deployment complete!
```

---

## 🔄 Full Development Workflow

### Day-to-Day Development

```bash
# Morning: Start a feature
git checkout -b feature/add-oauth-spotify

# Throughout day: Make changes, test locally
code src/FocusDeck.Server/Services/Integrations/SpotifyService.cs

# Before commit: Run BMAD cycle
./tools/BMAD-METHOD/bmad build   # Compile
./tools/BMAD-METHOD/bmad measure # Test
./tools/BMAD-METHOD/bmad adapt   # Format & analyze

# End of day: Commit & push
git add -A
git commit -m "Add Spotify OAuth integration"
git push origin feature/add-oauth-spotify
```

### Pull Request Workflow

```bash
# 1. Create PR on GitHub
# "Add Spotify OAuth integration"

# 2. GitHub Actions automatically runs:
#    ✅ BMAD Build (Windows + Linux)
#    ✅ BMAD Measure (tests + health checks)
#    ✅ BMAD Adapt (analysis + security scan)

# 3. Team reviews code
# "Looks good, just update the health check response"

# 4. Make changes locally
git add -A && git commit -m "Update health check response"
git push origin feature/add-oauth-spotify

# 5. GitHub Actions runs again
#    ✅ All checks pass

# 6. Merge to master
# GitHub automatically merges PR

# 7. GitHub Actions deploy
#    ✅ BMAD Deploy runs
#    ✅ Server updated to production
#    ✅ Health check passes
```

### Release Workflow

```bash
# 1. Feature merged to master
# 2. GitHub Actions:
#    - Builds all modules
#    - Runs all tests
#    - Analyzes code
#    - Deploys to production
# 3. Server updated automatically
# 4. Users get new features
```

---

## 🛠️ Manual Commands (When Needed)

### Build specific module

```bash
# Build only Server
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release

# Build only Desktop
dotnet build src/FocusDeck.Desktop/FocusDeck.Desktop.csproj -c Release

# Build only Mobile
dotnet build src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -f net8.0-android
```

### Run tests with filters

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run tests for specific class
dotnet test --filter "ClassName=StudySessionTests"
```

### Format code manually

```bash
# Format all code
dotnet format src/

# Format and check (no changes)
dotnet format --verify-no-changes

# Format specific file
dotnet format src/FocusDeck.Server/Controllers/StudySessionsController.cs
```

### Deploy without GitHub Actions

```bash
# Publish Release build
dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj \
  -c Release \
  -o ./publish/server \
  --self-contained

# Manual SSH deploy
scp -r ./publish/server/* focusdeck@192.168.1.110:/home/focusdeck/FocusDeck/src/FocusDeck.Server/bin/Release/net9.0/

# Remote restart
ssh focusdeck@192.168.1.110 "sudo systemctl restart focusdeck"

# Verify
curl https://focusdeck.909436.xyz/healthz
```

---

## 🐛 Troubleshooting

### Build fails: "Project X not found"

```bash
# Solution: Restore packages
dotnet restore
```

### Build fails: "SDK version not installed"

```bash
# List installed SDKs
dotnet --list-sdks

# Install .NET 9
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0

# Or use: https://dot.net/v1/dotnet-install.ps1 (Windows)
```

### Tests fail: "Database connection error"

```bash
# Solution: Tests use in-memory SQLite
# No action needed - should work automatically

# If still failing, check database path
ls -la focusdeck.db

# Delete and recreate
rm focusdeck.db
dotnet run --project src/FocusDeck.Server
```

### Health check timeout

```bash
# 1. Verify server is running
netstat -an | grep 5000

# 2. Start server if not running
dotnet run --project src/FocusDeck.Server &

# 3. Wait 5 seconds
sleep 5

# 4. Test health check
curl http://localhost:5000/healthz
```

### Deploy fails: "SSH key permission denied"

```bash
# Solution: Fix SSH key permissions
chmod 600 ~/.ssh/deploy_key

# Verify SSH works
ssh -i ~/.ssh/deploy_key focusdeck@192.168.1.110 "echo OK"
```

### GitHub Actions secrets not found

```bash
# Set secrets in GitHub:
# 1. Go to Settings → Secrets and variables → Actions
# 2. Add:
#    - DEPLOY_HOST = 192.168.1.110
#    - DEPLOY_USER = focusdeck
#    - DEPLOY_KEY = (private SSH key content)
```

---

## 📊 BMAD Configuration Reference

**File:** `.bmad-config.yml`

**Key sections:**

```yaml
# Project metadata
project_name: FocusDeck
version: 6b.2

# Build configuration
build:
  command: dotnet build
  configuration: Debug
  modules:
    - name: Server
      path: src/FocusDeck.Server
      publish: dotnet publish ... -c Release -o ./publish/server

# Health checks
measure:
  health_checks:
    - name: server_health
      url: http://localhost:5000/healthz
      expected_status: 200

# Code analysis
adapt:
  formatting:
    - run: dotnet format src/
  analysis:
    - run: dotnet build /warnaserror-

# Deployment targets
deploy:
  targets:
    - name: linux_server
      host: 192.168.1.110
      user: focusdeck
      systemd_service: focusdeck
```

---

## ✅ Checklist: Before Pushing Code

- [ ] Code changes made in feature branch
- [ ] `./tools/BMAD-METHOD/bmad build` passes ✅
- [ ] `dotnet test` passes all tests ✅
- [ ] `./tools/BMAD-METHOD/bmad measure` healthy ✅
- [ ] `./tools/BMAD-METHOD/bmad adapt` no critical issues ✅
- [ ] No hardcoded secrets or API keys
- [ ] Changes follow FocusDeck patterns from `copilot-instructions.md`
- [ ] Database changes include migrations
- [ ] Comments added for complex logic
- [ ] Ready to create PR 🚀

---

## 🔗 Related Documentation

- `.bmad-config.yml` - BMAD configuration
- `.github/workflows/focusdeck-bmad.yml` - CI/CD pipeline
- `.github/copilot-instructions.md` - FocusDeck AI coding standards
- `PLATFORM_ARCHITECTURE.md` - Multi-platform design
- `README.md` - Project overview

---

**Questions?** See `.bmad-config.yml` or `.github/workflows/focusdeck-bmad.yml` for detailed configuration.
