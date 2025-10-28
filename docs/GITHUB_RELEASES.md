# GitHub Release & Distribution Guide

## Overview

FocusDeck uses GitHub Actions for automated build, testing, and release management. This document explains the release workflow for all three platforms.

---

## 📦 Platforms & Artifacts

### Desktop (Windows 10+)
- **Output**: `FocusDeck-Desktop-v*.zip`
- **Contents**: Portable executable, dependencies, configuration templates
- **Build**: Windows Server (GitHub-hosted runner)
- **Deployment**: Direct download from GitHub Releases

### Mobile (Android 8+)
- **Output**: `FocusDeck-Mobile-v*.apk`
- **Contents**: Android application package
- **Build**: macOS (GitHub-hosted runner with Android SDK)
- **Deployment**: Direct download or ADB install

### Server (Linux)
- **Output**: `focusdeck-server-v*.tar.gz`
- **Contents**: Compiled binaries, configuration templates, systemd service files
- **Build**: Linux (GitHub-hosted runner)
- **Deployment**: Automated setup script or manual extraction

---

## 🔄 Release Process

### Option 1: Automated Release (Recommended)

#### Step 1: Create Version Tag
```bash
# From master branch
git tag -a v1.0.0 -m "Release v1.0.0: Study Timer Features"
git push origin v1.0.0
```

**GitHub Actions automatically:**
1. Detects `v*` tag push
2. Builds Desktop (Windows)
3. Builds Mobile (Android)
4. Builds Server (Linux)
5. Creates GitHub Release with all artifacts
6. Uploads binaries and checksums

#### Step 2: Review & Publish
- GitHub Release is created as **Draft**
- Review automated release notes
- Verify all artifacts present
- Click "Publish release" to make public

### Option 2: Manual Build & Release

```bash
# Build Desktop
dotnet publish src/FocusDock.App/FocusDock.App.csproj -c Release -o ./publish/desktop
cd publish/desktop && zip -r ../../FocusDeck-Desktop-v1.0.0.zip . && cd ../..

# Build Mobile
dotnet publish src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -f net8.0-android -c Release

# Build Server
dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -o ./publish/server
cd publish/server && tar -czf ../../focusdeck-server-v1.0.0.tar.gz . && cd ../..

# Upload to GitHub Releases manually
# https://github.com/dertder25t-png/FocusDeck/releases
```

---

## 🛠️ Workflow Files

### `.github/workflows/build-desktop.yml`
- **Trigger**: Push to master/develop, tags matching `v*`
- **Runner**: `windows-latest`
- **Steps**:
  1. Checkout code
  2. Setup .NET 8 SDK
  3. Restore NuGet packages
  4. Build Release configuration
  5. Publish WPF app
  6. Create ZIP archive
  7. Upload as artifact
  8. Create GitHub Release (if tagged)

### `.github/workflows/build-mobile.yml`
- **Trigger**: Push to master/develop, tags matching `v*`
- **Runner**: `macos-latest`
- **Steps**:
  1. Checkout code
  2. Setup .NET 8 SDK
  3. Setup Java 11 (for Android SDK)
  4. Restore NuGet packages
  5. Build Android Release APK
  6. Upload as artifact
  7. Create GitHub Release (if tagged)

### `.github/workflows/build-server.yml` (Coming)
- **Trigger**: Push to master/develop, tags matching `v*`
- **Runner**: `ubuntu-latest`
- **Steps**:
  1. Checkout code
  2. Setup .NET 8 SDK
  3. Restore NuGet packages
  4. Build Release configuration
  5. Publish server app
  6. Create tarball with config templates
  7. Upload as artifact
  8. Create GitHub Release (if tagged)

---

## 📥 Download Instructions

### For Users

**Desktop:**
1. Go to [Releases Page](https://github.com/dertder25t-png/FocusDeck/releases)
2. Click latest release
3. Download `FocusDeck-Desktop-v*.zip`
4. Extract to folder
5. Run `FocusDeck.exe`

**Mobile:**
1. Go to [Releases Page](https://github.com/dertder25t-png/FocusDeck/releases)
2. Click latest release
3. Download `FocusDeck-Mobile-v*.apk`
4. Transfer to Android device (download from release or via ADB)
5. Install (may need to enable "Unknown Sources")

**Server:**
1. SSH to Proxmox VM
2. Run automated setup:
   ```bash
   sudo bash <(curl -fsSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/setup-server.sh)
   ```
3. Download release during setup

---

## 🔒 Security & Verification

### Checksums
All releases include `SHA256.txt` with file hashes:
```bash
sha256sum -c SHA256.txt
```

### Code Signing (Future)
- Desktop APK can be signed with release keystore
- Server releases can include GPG signatures

### Verification Steps
```bash
# Download release and checksums
wget https://github.com/dertder25t-png/FocusDeck/releases/download/v1.0.0/FocusDeck-Desktop-v1.0.0.zip
wget https://github.com/dertder25t-png/FocusDeck/releases/download/v1.0.0/SHA256.txt

# Verify
sha256sum -c SHA256.txt
```

---

## 🐛 Troubleshooting Workflows

### Desktop Build Fails
- Check Windows SDK version requirements
- Verify .NET 8 compatibility
- Review WPF dependencies

### Mobile Build Fails
- Java version mismatch (requires Java 11)
- Android SDK not installed in environment
- MAUI framework incompatibility

### Release Not Created
- Ensure tag format is `v*` (e.g., `v1.0.0`)
- Check GitHub Actions tab for build failures
- Verify all artifacts were successfully built

### Artifacts Missing
- GitHub Actions logs show what went wrong
- Re-run failed job or manually build and upload

---

## 📋 Version Numbering

Follow Semantic Versioning: `MAJOR.MINOR.PATCH`

### Tags
- `v1.0.0` - Stable release
- `v1.0.0-beta` - Beta release (marked as pre-release)
- `v1.0.0-alpha` - Alpha release (marked as pre-release)

### Example Release Flow
```bash
# Development on master
git commit -m "Add study timer feature"

# Create release tag
git tag -a v1.1.0 -m "Add study timer: Start with 25-minute default, customizable intervals"

# Push to GitHub
git push origin master v1.1.0

# GitHub Actions triggers automatically...
```

---

## 🚀 Deployment

### Desktop Deployment
- Users download ZIP from releases
- Self-contained, no installation needed
- Check for updates manually or via app UI

### Mobile Deployment
- APK can be side-loaded to Android devices
- Future: Publish to Google Play Store (requires developer account)

### Server Deployment
- Automated setup script handles everything
- Or manual extraction and systemd configuration
- Nginx reverse proxy configured automatically

---

## 📞 CI/CD Pipeline Status

View workflow status:
- GitHub Actions Tab: https://github.com/dertder25t-png/FocusDeck/actions
- Click workflow name to see build logs
- Each step's output is visible for debugging

---

## Next Steps

1. ✅ GitHub Actions workflows configured
2. ⏳ Create first version tag to trigger workflows
3. ⏳ Verify all artifacts build successfully
4. ⏳ Test download and installation process
5. ⏳ Publish to app stores (future: Google Play, Microsoft Store)
