# Build Configuration Guide

This document describes the build system setup for FocusDeck across all platforms.

---

## 🏗️ Solution Structure

```
FocusDeck.sln
├── src/
│   ├── FocusDock.App/
│   │   ├── FocusDock.App.csproj          # Windows Desktop (WPF)
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── MainWindow.xaml / MainWindow.xaml.cs
│   │   └── Controls/
│   │
│   ├── FocusDeck.Mobile/
│   │   ├── FocusDeck.Mobile.csproj       # Android Mobile (MAUI)
│   │   ├── MauiProgram.cs                # DI & startup
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── AppShell.xaml
│   │   ├── Views/                        # XAML pages
│   │   ├── ViewModels/                   # MVVM ViewModels
│   │   └── Services/                     # Mobile platform services
│   │
│   ├── FocusDock.Core/
│   │   ├── FocusDock.Core.csproj         # Windows-specific core (net8.0-windows)
│   │   ├── Models/
│   │   └── Services/
│   │
│   ├── FocusDock.System/
│   │   ├── FocusDock.System.csproj       # Windows system integration
│   │   └── User32.cs                     # Win32 P/Invoke
│   │
│   ├── FocusDock.Data/
│   │   └── FocusDock.Data.csproj         # Data access (Windows-only currently)
│   │
│   └── FocusDeck.Services/
│       └── FocusDeck.Services.csproj     # Cloud services, audio, encryption
│
└── .github/workflows/
    ├── build-desktop.yml                 # GitHub Actions: Windows build
    └── build-mobile.yml                  # GitHub Actions: Android build
```

---

## 🎯 Target Frameworks

### Desktop (FocusDock.App)
```xml
<TargetFrameworks>net8.0-windows10.0.19041.0</TargetFrameworks>
```
- **Platform**: Windows only (uses WPF, Win32 P/Invoke)
- **Minimum OS**: Windows 10 Build 19041
- **Runtime**: .NET 8

### Mobile (FocusDeck.Mobile)
```xml
<TargetFrameworks>net8.0-windows10.0.19041.0;net8.0-android;</TargetFrameworks>
```
- **Platforms**: Windows + Android (currently only Android is deployed)
- **Android Min SDK**: 21 (Android 5.0)
- **Android Target SDK**: 34 (Android 14)
- **Runtime**: .NET 8 MAUI

### Shared Libraries (Core, System, Data, Services)
```xml
<TargetFrameworks>net8.0-windows10.0.19041.0</TargetFrameworks>
```
- **Platform**: Windows only (contain WPF/Win32 dependencies)
- **Note**: Mobile apps DON'T reference these for cross-platform compatibility

---

## 🔨 Build Commands

### Full Solution
```bash
# Restore all NuGet packages
dotnet restore

# Build all (Debug)
dotnet build

# Build all (Release)
dotnet build -c Release

# Build and show warnings only
dotnet build /warnaserror-
```

### Desktop Only
```bash
# Restore
dotnet restore src/FocusDock.App/FocusDock.App.csproj

# Build
dotnet build src/FocusDock.App/FocusDock.App.csproj -c Release

# Publish (self-contained)
dotnet publish src/FocusDock.App/FocusDock.App.csproj -c Release -o ./publish/desktop

# Create ZIP
cd publish/desktop && zip -r ../../FocusDeck-Desktop.zip . && cd ../..
```

### Mobile Only
```bash
# Restore
dotnet restore src/FocusDeck.Mobile/FocusDeck.Mobile.csproj

# Build (Debug)
dotnet build src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -c Debug

# Build APK (Release)
dotnet publish src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -f net8.0-android -c Release

# Output location: src/FocusDeck.Mobile/bin/Release/net8.0-android/*.apk
```

### Individual Projects
```bash
# Core
dotnet build src/FocusDock.Core/FocusDock.Core.csproj -c Release

# System Integration
dotnet build src/FocusDock.System/FocusDock.System.csproj -c Release

# Data Access
dotnet build src/FocusDock.Data/FocusDock.Data.csproj -c Release

# Services
dotnet build src/FocusDeck.Services/FocusDeck.Services.csproj -c Release
```

---

## 🧪 Testing

### Run Unit Tests
```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/FocusDeck.Tests.csproj

# Run only tests matching pattern
dotnet test --filter "ClassName"

# Generate coverage report
dotnet test /p:CollectCoverageEnabled=true
```

---

## 📦 Publishing

### Desktop Distribution
```bash
# Publish to folder
dotnet publish src/FocusDock.App/FocusDock.App.csproj \
  -c Release \
  -o ./publish/desktop \
  --self-contained \
  -r win-x64

# Create archive
cd publish/desktop
zip -r ../../FocusDeck-Desktop-v1.0.0.zip .
cd ../..
```

### Mobile Distribution
```bash
# Build APK
dotnet publish src/FocusDeck.Mobile/FocusDeck.Mobile.csproj \
  -f net8.0-android \
  -c Release

# APK location: 
# src/FocusDeck.Mobile/bin/Release/net8.0-android/com.focusdeck.*.apk
```

### Server Distribution
```bash
# Publish to folder
dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj \
  -c Release \
  -o ./publish/server \
  --self-contained \
  -r linux-x64

# Create tarball with config
cd publish/server
tar -czf ../../focusdeck-server-v1.0.0.tar.gz .
cd ../..
```

---

## 🔍 Build Configuration Files

### Desktop (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="..." Version="..." />
  </ItemGroup>
</Project>
```

### Mobile (.csproj)
```xml
<Project Sdk="Microsoft.Maui.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0-windows10.0.19041.0;net8.0-android;</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <LangVersion>12.0</LangVersion>
    
    <!-- Android specific -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('net8.0-android')) != ''">21</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('net8.0-windows')) != ''">10.0.19041</SupportedOSPlatformVersion>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.*" />
  </ItemGroup>
</Project>
```

---

## 🚀 CI/CD Pipeline

### GitHub Actions Triggers

**Desktop Build:**
- Trigger: Push to `master` or `develop`
- Trigger: Tag push matching `v*`
- Runs: `windows-latest`
- Outputs: `FocusDeck-Desktop-v*.zip`

**Mobile Build:**
- Trigger: Push to `master` or `develop`
- Trigger: Tag push matching `v*`
- Runs: `macos-latest`
- Outputs: `FocusDeck-Mobile-v*.apk`

**Release Creation:**
- Trigger: Tag push matching `v*`
- Creates GitHub Release
- Uploads all artifacts
- Auto-generates release notes

### Manual Workflow Dispatch
```bash
# Trigger workflow manually via GitHub CLI
gh workflow run build-desktop.yml
gh workflow run build-mobile.yml
```

---

## 📊 Build Output Locations

### Debug Build
```
src/
├── FocusDock.App/bin/Debug/net8.0-windows10.0.19041.0/
├── FocusDeck.Mobile/bin/Debug/net8.0-windows10.0.19041.0/
├── FocusDeck.Mobile/bin/Debug/net8.0-android/
└── ...
```

### Release Build
```
src/
├── FocusDock.App/bin/Release/net8.0-windows10.0.19041.0/
├── FocusDeck.Mobile/bin/Release/net8.0-android/*.apk
└── ...

publish/
├── desktop/FocusDeck.exe
├── server/FocusDeck.Server
└── ...
```

---

## 🔧 Development Setup

### Prerequisites
```bash
# .NET 8 SDK (includes runtime)
dotnet --version
# Should show 8.0.x

# Git
git --version

# (Optional) Visual Studio 2022 or VS Code
# (Optional) Android SDK for mobile dev
```

### Project Setup
```bash
# Clone
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck

# Restore
dotnet restore

# Build
dotnet build

# Verify (should show 0 errors)
```

### Development Workflow
```bash
# Create feature branch
git checkout -b feature/awesome-feature

# Edit code
# Commit changes
git commit -m "Add awesome feature"

# Push to GitHub
git push origin feature/awesome-feature

# Create Pull Request on GitHub
```

---

## 🐛 Common Build Issues

### Issue: "Project X not found"
```bash
# Solution: Restore packages
dotnet restore
```

### Issue: "SDK not installed"
```bash
# Solution: Install .NET 8
dotnet --list-sdks
# If not present, download from https://dotnet.microsoft.com/download/dotnet/8.0
```

### Issue: "MAUI workload not installed"
```bash
# Solution: Install MAUI workload
dotnet workload restore
```

### Issue: "Windows SDK version not available"
```bash
# Desktop requires Windows 10 SDK 19041
# Install via Visual Studio Installer
# Or: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
```

---

## 📈 Build Performance

### Parallel Build
```bash
# Use multiple CPU cores
dotnet build -m
```

### Incremental Build
```bash
# Only rebuild changed projects
dotnet build
```

### Clean Build
```bash
# Remove all build artifacts
dotnet clean
dotnet build
```

### Build Time Optimization
```bash
# Skip tests during development
dotnet build --no-restore

# Use Release configuration for faster iterations
dotnet build -c Release
```

---

## 📝 Versioning

### Version Format
`MAJOR.MINOR.PATCH-PRERELEASE+BUILD`

Example: `1.0.0`, `1.1.0-beta`, `1.0.1-rc1`

### Update Version
```bash
# Edit in project files
# Desktop: src/FocusDock.App/FocusDock.App.csproj
# Mobile: src/FocusDeck.Mobile/FocusDeck.Mobile.csproj

# Look for:
<PropertyGroup>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
  <InformationalVersion>1.0.0</InformationalVersion>
</PropertyGroup>
```

---

## 🔗 Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/)
- [MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [WPF Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [GitHub Actions](https://github.com/features/actions)
