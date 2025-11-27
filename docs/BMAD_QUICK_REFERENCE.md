# 🚀 BMAD-METHOD Quick Reference Card

## One-Liners

```bash
# Complete cycle (local development)
./tools/BMAD-METHOD/bmad run

# Just build
./tools/BMAD-METHOD/bmad build

# Just test (measure)
./tools/BMAD-METHOD/bmad measure

# Just format & analyze (adapt)
./tools/BMAD-METHOD/bmad adapt

# Just deploy
./tools/BMAD-METHOD/bmad deploy
```

## Daily Developer Fl

| Step | Command | Time |
|------|---------|------|
| 1️⃣ Build | `./tools/BMAD-METHOD/bmad build` | 60s |
| 2️⃣ Test | `./tools/BMAD-METHOD/bmad measure` | 120s |
| 3️⃣ Format | `./tools/BMAD-METHOD/bmad adapt` | 30s |
| 4️⃣ Push | `git push origin feature/my-feature` | 5s |
| 5️⃣ Auto-Deploy (on master) | GitHub Actions | 5m |

**Total local time: ~4 minutes**

---

## Build Modules (Dependency Order)

```
1. Domain (no deps)
   ↓
2. Persistence (→ Domain)
   ↓
3. Contracts (no deps)
   ↓
4. Shared (→ Contracts)
   ↓
5. Server (→ all above) ← Linux server target
   ↓
6. Desktop (→ Shared) ← Windows only
   ↓
7. Mobile (→ Shared) ← Android only
```

---

## GitHub Actions Matrix

**Windows runners:**
- Desktop build
- Mobile build  
- Shared/Domain/Contracts

**Linux runners:**
- Server build
- Full test suite
- Health checks
- Deployment

**Triggers:**
- `push`: master, develop
- `pull_request`: master, develop
- `tag`: v*.* (releases)

---

## Health Checks

```
✅ Server Health
   GET http://localhost:5000/healthz
   → {"ok":true, "time":"..."}

✅ API Services
   GET http://localhost:5000/api/services
   → [{id, service, configured, ...}]

✅ Remote Control
   GET http://localhost:5000/v1/remote/actions
   → [{id, action, status, ...}]
```

---

## Test Categories

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests only
dotnet test --filter "Category=Integration"

# All tests
dotnet test

# Specific class
dotnet test --filter "ClassName=StudySessionTests"
```

---

## Deployment Secrets (GitHub)

Required for auto-deploy:

```
DEPLOY_HOST     = 192.168.1.110
DEPLOY_USER     = focusdeck
DEPLOY_KEY      = (SSH private key)
```

Set in: **Settings → Secrets and variables → Actions**

---

## Local Server Launch

```bash
# Start server (default: http://localhost:5000)
dotnet run --project src/FocusDeck.Server

# With environment variables
ASPNETCORE_ENVIRONMENT=Development \
DATABASE_URL=Data Source=focusdeck.db \
dotnet run --project src/FocusDeck.Server
```

---

## Common Issues → Solutions

| Issue | Command |
|-------|---------|
| Build fails | `dotnet restore && ./tools/BMAD-METHOD/bmad build` |
| Tests fail | `dotnet test --logger "console;verbosity=detailed"` |
| Health check timeout | `dotnet run --project src/FocusDeck.Server &` |
| Format issues | `dotnet format src/` |
| Vulnerable packages | `dotnet list package --vulnerable` |
| Deploy permission denied | `chmod 600 ~/.ssh/deploy_key` |

---

## Feature Branch Lifecycle

```bash
# 1. Create branch
git checkout -b feature/my-feature

# 2. Make changes
# ... edit code ...

# 3. Run local BMAD
./tools/BMAD-METHOD/bmad run

# 4. Commit & push
git add -A
git commit -m "Add my feature"
git push origin feature/my-feature

# 5. GitHub Actions runs automatically
#    ✅ Build (Windows + Linux)
#    ✅ Measure (tests + health)
#    ✅ Adapt (format + analysis)

# 6. Create PR, get review

# 7. Merge to master
# (merge commit on GitHub)

# 8. GitHub Actions auto-deploys
#    ✅ Build
#    ✅ Measure
#    ✅ Adapt
#    ✅ Deploy to production

# Result: Feature live in production! 🚀
```

---

## Configuration Files

| File | Purpose |
|------|---------|
| `.bmad-config.yml` | BMAD module definitions, health checks, deployment targets |
| `.github/workflows/focusdeck-bmad.yml` | GitHub Actions CI/CD pipeline (Build → Measure → Adapt → Deploy) |
| `.github/copilot-instructions.md` | AI coding standards & patterns |
| `BMAD_DEVELOPER_GUIDE.md` | Detailed guide (this repo) |

---

## Deployment Flow (Auto)

```
🔄 Push to master
   ↓
📦 Build (all platforms)
   ✅ Windows: Desktop, Mobile
   ✅ Linux: Server
   ↓
🧪 Measure (tests + health)
   ✅ Unit tests (523 passed)
   ✅ Integration tests (45 passed)
   ✅ Coverage: 78%
   ✅ Health check: 200 OK
   ↓
🔧 Adapt (format + analysis)
   ✅ Code formatted
   ✅ No vulnerabilities
   ✅ No outdated packages
   ↓
🚀 Deploy
   ✅ Publish server
   ✅ SSH to Linux
   ✅ Update binaries
   ✅ Restart systemd
   ✅ Verify health
   ↓
✨ Live in production!
```

**Total time: ~5 minutes**

---

## Performance Targets

| Metric | Target | Critical |
|--------|--------|----------|
| Build time | 60s | 120s |
| Test time | 120s | 300s |
| API P95 response | 500ms | - |
| API P99 response | - | 1000ms |
| Code coverage | 70% | 50% |
| Health check | <5s | <10s |

---

## Platform Support

| Platform | Language | Status |
|----------|----------|--------|
| **Server** | .NET 9 | ✅ Linux, Windows, Mac |
| **Desktop** | .NET 9 WPF | ✅ Windows only |
| **Mobile** | .NET 8 MAUI | ✅ Android only |
| **Legacy** | .NET 8 WPF | ✅ Windows only |

---

## Emergency Commands

```bash
# Rollback latest deploy (SSH to server)
sudo systemctl restart focusdeck

# Check service status
sudo systemctl status focusdeck

# Watch live logs
journalctl -u focusdeck -f

# Force rebuild everything
dotnet clean && dotnet build

# Delete all build artifacts
rm -rf src/*/bin src/*/obj ./publish ./test-results
```

---

**Need detailed help?** See `BMAD_DEVELOPER_GUIDE.md`

**Questions?** Check `.bmad-config.yml` or `.github/workflows/focusdeck-bmad.yml`
