# FocusDeck Platform Architecture

This document clarifies the multi-platform structure of FocusDeck to help contributors understand where to implement features based on the target operating system.

## 📐 Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    FocusDeck Ecosystem                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐   ┌──────────────────┐              │
│  │ FocusDeck.Mobile│   │ FocusDeck.Desktop│              │
│  │  (Android App)  │   │   (Windows WPF)  │              │
│  │  📱 MAUI        │   │   🪟 .NET 9      │              │
│  └────────┬────────┘   └────────┬─────────┘              │
│           │                     │                         │
│           │  HTTPS/SignalR      │  HTTPS/SignalR         │
│           └──────────┬──────────┘                         │
│                      │                                     │
│              ┌───────▼────────┐                           │
│              │ FocusDeck.Server│                          │
│              │  (Linux Server) │                          │
│              │  🐧 .NET 9      │                          │
│              └────────────────┘                           │
│                                                            │
│  Additional Windows Apps:                                 │
│  ┌──────────────────┐                                    │
│  │  FocusDock.App   │  (Separate Windows application)    │
│  │  🪟 .NET 8       │                                    │
│  └──────────────────┘                                    │
│                                                            │
└─────────────────────────────────────────────────────────────┘
```

## 🗂️ Project Structure by Platform

### 🐧 Linux Server (Cross-Platform)

**Project:** `src/FocusDeck.Server`
- **Target:** `net9.0` (cross-platform)
- **OS:** Linux (can also run on Windows/Mac)
- **Purpose:** REST API, SignalR hub, business logic
- **Dependencies:**
  - FocusDeck.Domain
  - FocusDeck.Persistence
  - FocusDeck.Contracts
  - PostgreSQL/SQLite

**Can build/run on:** ✅ Linux, ✅ Windows, ✅ macOS

**Features:**
- REST API endpoints
- SignalR real-time communication
- Database management
- Authentication/Authorization
- Focus session logic (server-side)

---

### 📱 Android Mobile App

**Project:** `src/FocusDeck.Mobile`
- **Target:** `net8.0-android` (MAUI)
- **OS:** Android 8.0+
- **Purpose:** Mobile client app
- **Dependencies:**
  - Microsoft.Maui
  - Platform-specific Android APIs
  - FocusDeck.Shared

**Can build/run on:** ✅ Windows (with MAUI workload), ✅ macOS (with MAUI workload)

**Platform-Specific Features:**
- Accelerometer sensor access
- Screen state monitoring (via PowerManager)
- Local notifications
- Do Not Disturb integration
- Focus signal submission to server

**Implementation Location:**
- `src/FocusDeck.Mobile/Services/SensorMonitorService.cs` - Sensor monitoring
- `src/FocusDeck.Mobile/Services/FocusNotificationService.cs` - Notifications
- Android-specific: `src/FocusDeck.Mobile/Platforms/Android/`

---

### 🪟 Windows Desktop App (WPF)

**Project:** `src/FocusDeck.Desktop`
- **Target:** `net9.0-windows` (WPF)
- **OS:** Windows 10+ (version 19041+)
- **Purpose:** Desktop client app with WPF UI
- **Dependencies:**
  - WPF framework
  - Windows API interop
  - FocusDeck.Shared

**Can build/run on:** ✅ Windows only

**Platform-Specific Features:**
- Keyboard/mouse idle detection (GetLastInputInfo)
- Ambient noise monitoring (NAudio)
- WPF overlay windows (dim effect)
- System tray integration
- Focus signal submission to server

**Implementation Location:**
- `src/FocusDeck.Desktop/Services/ActivityMonitorService.cs` - Activity monitoring
- `src/FocusDeck.Desktop/Services/FocusOverlayService.cs` - Overlay UI
- `src/FocusDeck.Desktop/Views/` - WPF views

---

### 🪟 Windows Desktop App (FocusDock)

**Project:** `src/FocusDock.App`
- **Target:** `net8.0-windows` (WPF + WinForms)
- **OS:** Windows 10+
- **Purpose:** Alternative Windows desktop app (appears to be separate from FocusDeck.Desktop)
- **Dependencies:**
  - WPF framework
  - Windows Forms
  - FocusDock.Core
  - FocusDock.Data

**Can build/run on:** ✅ Windows only

**Note:** This appears to be a separate application line. Determine if Focus Mode features should be added here as well.

---

## 🔗 Shared/Common Projects

### Cross-Platform Shared Code

**Projects:**
- `src/FocusDeck.Shared` - Shared models and utilities
- `src/FocusDeck.SharedKernel` - Core domain primitives
- `src/FocusDeck.Domain` - Domain entities
- `src/FocusDeck.Contracts` - DTOs and contracts
- `src/FocusDeck.Persistence` - Data access layer

**Target:** Multi-targeting (net8.0, net9.0)
**Can use from:** All platforms

---

## 🎯 Focus Mode Implementation Status

### ✅ Server (Complete)
**Location:** `src/FocusDeck.Server/Controllers/v1/FocusController.cs`
- REST API endpoints
- Distraction detection logic
- Recovery suggestion logic
- SignalR event broadcasting
- Database schema (FocusSessions table)

**Status:** Fully implemented and tested (12 unit tests passing)

---

### 🚧 Desktop (Stub)
**Location:** `src/FocusDeck.Desktop/Services/`
- `ActivityMonitorService.cs` - **Needs implementation on Windows**
  - GetLastInputInfo API calls
  - NAudio integration for ambient noise
  - Signal submission to server
- `FocusOverlayService.cs` - **Needs implementation on Windows**
  - WPF overlay windows
  - Recovery banner UI
  - Action button handlers

**Status:** Stub with detailed implementation guide

**Requires:** Windows development machine with:
- Visual Studio 2022+
- .NET 9.0 SDK
- WPF workload
- NAudio NuGet package

---

### 🚧 Mobile (Stub)
**Location:** `src/FocusDeck.Mobile/Services/`
- `SensorMonitorService.cs` - **Needs implementation with MAUI**
  - Accelerometer API integration
  - Screen state monitoring (Android PowerManager)
  - Light sensor access
  - Signal submission to server
- `FocusNotificationService.cs` - **Needs implementation with MAUI**
  - Local notifications
  - Action sheets for recovery suggestions
  - Do Not Disturb integration

**Status:** Stub with detailed implementation guide

**Requires:** Development machine with:
- Visual Studio 2022+ or VS Code
- .NET 8.0 MAUI workload
- Android SDK (for Android)
- iOS SDK (for iOS, if needed)

---

## 🛠️ Development Workflow by Feature

### Adding a New Feature

#### 1. Server-Side Feature (Linux)
```bash
# Can develop on Linux, Windows, or macOS
cd src/FocusDeck.Server
dotnet build
dotnet test ../../tests/FocusDeck.Server.Tests
```

#### 2. Android Feature (Mobile)
```bash
# Requires Windows/macOS with MAUI workload
cd src/FocusDeck.Mobile
dotnet build -f net8.0-android
# Deploy to emulator or device
```

#### 3. Windows Desktop Feature
```bash
# Requires Windows
cd src/FocusDeck.Desktop
dotnet build
dotnet run
```

---

## 📝 Contributing Guidelines

### Where to Implement Features

| Feature Type | Project | Platform Required | Can Test On |
|-------------|---------|-------------------|-------------|
| REST API | FocusDeck.Server | Any (Linux/Win/Mac) | Any |
| Database | FocusDeck.Persistence | Any | Any |
| Mobile UI | FocusDeck.Mobile | Win/Mac with MAUI | Android device/emulator |
| Desktop UI | FocusDeck.Desktop | Windows | Windows |
| Shared Logic | FocusDeck.Shared | Any | Any |

### Platform-Specific Code Locations

**Android-Specific Code:**
```
src/FocusDeck.Mobile/Platforms/Android/
  ├── MainActivity.cs
  ├── MainApplication.cs
  └── Services/        # Platform-specific services
```

**Windows-Specific Code (Desktop):**
```
src/FocusDeck.Desktop/
  ├── Services/        # Windows API interop
  ├── Views/           # WPF views
  └── App.xaml         # WPF application
```

**Server Code (Cross-Platform):**
```
src/FocusDeck.Server/
  ├── Controllers/     # REST endpoints
  ├── Hubs/           # SignalR hubs
  └── Services/       # Business logic
```

---

## 🔍 How to Identify Platform Requirements

### By Project Name
- **FocusDeck.Server** → Cross-platform (prefer Linux for production)
- **FocusDeck.Mobile** → Android (MAUI required)
- **FocusDeck.Desktop** → Windows WPF
- **FocusDock.App** → Windows WPF/WinForms

### By .csproj TargetFramework
```xml
<!-- Cross-platform -->
<TargetFramework>net9.0</TargetFramework>

<!-- Windows-specific -->
<TargetFramework>net9.0-windows</TargetFramework>

<!-- Android-specific -->
<TargetFrameworks>net8.0-android</TargetFrameworks>
```

### By Dependencies
- **Uses WPF** → Windows only
- **Uses MAUI** → Android/iOS (requires workload)
- **Uses NAudio** → Windows only (for audio)
- **Uses PowerManager** → Android only

---

## 🚀 CI/CD Considerations

### Build Matrix

```yaml
# Example GitHub Actions matrix
strategy:
  matrix:
    include:
      - os: ubuntu-latest
        project: FocusDeck.Server
        
      - os: windows-latest
        project: FocusDeck.Desktop
        
      - os: windows-latest  # or macos-latest
        project: FocusDeck.Mobile
```

### Platform-Specific Build Steps

**Server (Linux):**
```bash
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj
dotnet test tests/FocusDeck.Server.Tests
```

**Desktop (Windows):**
```bash
dotnet build src/FocusDeck.Desktop/FocusDeck.Desktop.csproj -c Release
```

**Mobile (Android):**
```bash
dotnet build src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -f net8.0-android -c Release
```

---

## 📚 Additional Resources

- **Integration Guide:** [FOCUS_MODE_INTEGRATION_GUIDE.md](./FOCUS_MODE_INTEGRATION_GUIDE.md)
- **Implementation Details:** [FOCUS_MODE_IMPLEMENTATION.md](./FOCUS_MODE_IMPLEMENTATION.md)
- **Server Setup:** [SERVER_SETUP.md](./SERVER_SETUP.md)
- **Quick Start:** [QUICKSTART.md](./QUICKSTART.md)

---

## ⚠️ Common Pitfalls

1. **Building Mobile on Linux** → Won't work (needs MAUI workload on Windows/Mac)
2. **Building Desktop on Linux** → Won't work (needs Windows)
3. **Running Server on Windows** → Works fine (but production is Linux)
4. **Missing Platform Code** → Check `Platforms/` subfolder in MAUI projects

---

## 🎯 Summary

- **FocusDeck.Server** = Linux server (cross-platform .NET)
- **FocusDeck.Mobile** = Android app (MAUI on Windows/Mac)
- **FocusDeck.Desktop** = Windows WPF app
- **FocusDock.App** = Separate Windows app

Each platform requires appropriate development environment and tooling. Always check the project's `TargetFramework` to understand platform requirements.
