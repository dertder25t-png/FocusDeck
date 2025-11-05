# FocusDeck 🎯

**Smart productivity suite with cloud synchronization for desktop, mobile, and web**

> Last Updated: noveber 2025

FocusDeck is a cross-platform productivity suite combining window management, study timers, session tracking, and cloud synchronization. Focus on what matters while we handle the rest.

---

## 🚀 Quick Start

### 📥 Desktop (Windows 10+)

1. [Download Latest Release](https://github.com/dertder25t-png/FocusDeck/releases) → `FocusDeck-Desktop-*.zip`
2. Extract and run `FocusDeck.exe`
3. **Requirements**: Windows 10+ (version 19041), .NET 8.0 Runtime

### 📱 Mobile (Android 8+)

1. [Download Latest Release](https://github.com/dertder25t-png/FocusDeck/releases) → `FocusDeck-Mobile-*.apk`
2. Enable "Install from Unknown Sources"
3. Install APK
4. **Requirements**: Android 8.0+, 50MB storage

### 🌐 Web Application (Browser-Based SaaS)

Access FocusDeck through your web browser - no installation required!

```
https://your-focusdeck-domain.com/app
```

**Features:**
- 🎓 **Lecture Companion** - Upload, transcribe, and process lectures
- ⚡ **Focus Sessions** - Start and track focus sessions with real-time updates
- 📝 **AI-Verified Notes** - Verify and enhance your study notes
- 🎨 **Design Assist** - Generate design ideas and concepts
- 📊 **Analytics** - View your productivity insights

**Technology:**
- React + TypeScript + Tailwind CSS
- Real-time updates via SignalR WebSockets
- Responsive design for desktop and tablet
- Dark mode by default

See [src/FocusDeck.WebApp/README.md](src/FocusDeck.WebApp/README.md) for development instructions.

### 🖥️ Server (Self-Hosted Sync Backend)

Deploy your own sync server in **one command** using our interactive script:

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/easy-setup.sh | sudo bash
```

This script will guide you through the setup and is the recommended way to install.

The script will:
- ✅ Install .NET 9.0 and Git automatically
- ✅ Prompt you to install Cloudflare Tunnel (cloudflared) on the same server (Recommended for security)
- ✅ Clone and build FocusDeck
- ✅ Generate secure keys
- ✅ Configure and start a systemd service for the server
- ✅ Provide custom instructions on how to complete your Cloudflare setup.

📖 **Quick Setup Guide**: [SIMPLE_SETUP.md](SIMPLE_SETUP.md)  
📖 **Advanced Configuration**: [docs/CLOUDFLARE_DEPLOYMENT.md](docs/CLOUDFLARE_DEPLOYMENT.md)

**Requirements**: Ubuntu 20.04+/Debian 10+, 512MB RAM, 1GB disk, Cloudflare domain

---

## 🌟 What is FocusDeck?

- **🪟 Window Management** - Auto-organize apps into layouts, save workspaces
- **⏱️ Study Timers** - Configurable Pomodoro sessions with tracking
- **📊 Analytics** - Visualize study patterns and productivity
- **☁️ Cloud Sync** - Seamlessly sync across Windows, Android, and web
- **📅 Calendar Integration** - Google Calendar & Canvas LMS
- **✓ Task Management** - To-do lists with priorities
- **🤖 AI Planner** - Auto-generate study schedules

---

## 📊 Project Status

| Component | Phase | Status |
|-----------|-------|--------|
| **Desktop (WPF)** | 1-5 | ✅ Complete |
| **Server API** | 6a | ✅ Complete |
| **Web Admin UI** | 6a | ✅ Complete |
| **Mobile (MAUI)** | 6b | ⏳ Week 2 |

**Current Focus**: Phase 6b Week 2 - Mobile Study Timer Implementation

---

## ✨ Features

### 🪟 Window Management (Phase 1)

- **Auto-Collapsing Dock** - Edge-mounted UI (top/bottom/left/right)
- **Real-Time Window Tracking** - Live updates via Win32 API
- **Pin System** - Keep windows persistent across layouts
- **Layout Templates** - Two-Column, Three-Column, Grid 2x2
- **Workspace Manager** - Save & restore complete desktop states
- **Multi-Monitor Support** - Per-monitor layout management
- **"Park Today"** - End-of-day workspace save
- **Time-Based Automations** - Schedule window arrangements
- **Stale Window Detection** - Reminders for inactive windows

### 📅 Calendar & Tasks (Phase 2)

- **Google Calendar API** - OAuth2 integration ready
- **Canvas LMS** - Assignment tracking and auto-sync
- **To-Do Lists** - Create, prioritize, complete tasks
- **Priority Levels** - Low/Medium/High/Urgent
- **Due Date Tracking** - Overdue alerts
- **Bulk Operations** - Filter, clear completed
- **Auto-Sync** - Every 15 minutes (configurable)
- **Quick Statistics** - "5/12 completed • 3 active • 1 overdue"

### 🤖 AI Study Planner (Phase 3)

- **Auto-Generate Study Plans** - From calendar assignments
- **Smart Scheduling** - Distribute hours across available days
- **Pomodoro Recommendations** - Optimal session lengths
- **Session History Tracking** - Effectiveness ratings (1-5)
- **Performance Analytics** - "8 sessions • 12.5h • 4.2/5"
- **Fast Generation** - Complete plans in <500ms

### ☁️ Cloud Sync Backend (Phase 6a) ✅

- **REST API** - Full CRUD operations for decks/sessions
- **Web Admin Panel** - Beautiful responsive UI with purple gradient theme
- **Dashboard** - Real-time statistics and overview
- **Deck Management** - Create/edit/delete decks and cards
- **Data Export/Import** - JSON backup and restore
- **API Documentation** - Interactive endpoint reference
- **Self-Hosted** - Full data ownership and privacy
- **Zero Dependencies** - Vanilla HTML/CSS/JavaScript

### 📱 Mobile App (Phase 6b - In Progress)

- ✅ **MAUI Foundation** - 4-tab navigation (Study/History/Analytics/Settings)
- ✅ **Study Timer** - Pomodoro presets (15/25/45/60 min), custom times
- ✅ **Timer Controls** - Start/Pause/Stop/Reset with haptic feedback
- ✅ **Session Notes** - Add notes to study sessions
- ✅ **Progress Visualization** - Circular progress bar with percentage
- 🔄 **SQLite Database** - Local session storage (Week 3)
- ⏳ **Cloud Sync** - Sync with server API (Week 3)

---

## 🏗️ Architecture

### Desktop (WPF)
- **.NET 8.0** - Windows Presentation Foundation
- **Win32 P/Invoke** - Native window management
- **JSON Persistence** - Local data storage
- **RESTful API Client** - Server communication

### Mobile (MAUI)
- **.NET MAUI** - Cross-platform framework
- **SQLite-net-pcl** - Local database
- **MVVM Pattern** - Clean architecture
- **Dependency Injection** - Service-oriented design

### Server (ASP.NET Core)
- **.NET 9.0** - Latest performance improvements
- **Layered Architecture** - Domain, Persistence, Contracts, SharedKernel
- **JWT Authentication** - Access & refresh tokens
- **Serilog** - Structured logging with correlation IDs
- **OpenTelemetry** - Distributed tracing (HTTP, EF Core, SignalR)
- **Hangfire** - Background job processing with PostgreSQL
- **SignalR** - Real-time notifications hub
- **API Versioning** - /v1/* endpoints with Swagger documentation
- **Health Checks** - Database and file system monitoring
- **PostgreSQL/SQLite** - Dual database support

### Web UI
- **Vanilla JavaScript** - Zero framework dependencies
- **CSS Grid & Flexbox** - Responsive layout
- **LocalStorage** - Client-side settings
- **Fetch API** - REST communication

---

## 📚 Documentation

### Getting Started
- [00_START_HERE.md](00_START_HERE.md) - Project overview
- [QUICKSTART.md](QUICKSTART.md) - Quick setup guide
- [API_SETUP_GUIDE.md](API_SETUP_GUIDE.md) - Calendar & LMS integration

### Server Setup
- [SERVER_SETUP.md](SERVER_SETUP.md) - One-command installation & manual setup
- [WEB_UI_GUIDE.md](WEB_UI_GUIDE.md) - Web admin panel guide
- [SELFHOSTED_SETUP_GUIDE.md](docs/SELFHOSTED_SETUP_GUIDE.md) - Advanced configuration

### Development
- [DEVELOPMENT.md](DEVELOPMENT.md) - Developer guide
- [MAUI_ARCHITECTURE.md](docs/MAUI_ARCHITECTURE.md) - Mobile architecture
- [DATABASE_API_REFERENCE.md](docs/DATABASE_API_REFERENCE.md) - API endpoints

### Project Status
- [PROJECT_STATUS.md](PROJECT_STATUS.md) - Current progress
- [VISION_ROADMAP.md](VISION_ROADMAP.md) - Future plans
- [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md) - All docs

---

## ⚙️ Server Configuration

### Required Configuration Keys

Create an `appsettings.json` file in the server directory with these keys:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=focusdeck.db",
    "HangfireConnection": "Host=localhost;Database=hangfire;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-characters-long",
    "Issuer": "FocusDeck.Server",
    "Audience": "FocusDeck.Client",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
```

### Key Configuration Options

| Key | Description | Default | Required |
|-----|-------------|---------|----------|
| `ConnectionStrings:DefaultConnection` | Main database (SQLite or PostgreSQL) | `Data Source=focusdeck.db` | Yes |
| `ConnectionStrings:HangfireConnection` | Hangfire jobs database (PostgreSQL only) | Falls back to DefaultConnection | No |
| `Jwt:Secret` | JWT signing key (min 32 chars) | - | Yes |
| `Jwt:Issuer` | Token issuer identifier | `FocusDeck.Server` | Yes |
| `Jwt:Audience` | Token audience identifier | `FocusDeck.Client` | Yes |
| `Jwt:AccessTokenExpirationMinutes` | Access token lifetime | `60` | No |
| `Jwt:RefreshTokenExpirationDays` | Refresh token lifetime | `7` | No |

### Environment Variables

You can also use environment variables (overrides appsettings.json):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=focusdeck;Username=postgres;Password=yourpassword"
export Jwt__Secret="your-production-secret-key-32-chars"
export ASPNETCORE_ENVIRONMENT="Production"
```

### API Endpoints

- **Swagger UI**: `https://your-domain.com/swagger`
- **Health Check**: `https://your-domain.com/v1/system/health`
- **Hangfire Dashboard**: `https://your-domain.com/hangfire` (requires auth)
- **SignalR Hub**: `wss://your-domain.com/hubs/notifications`

For complete API documentation, see [API_README.md](src/FocusDeck.Server/API_README.md)

---

## 🛠️ Development Setup

### Prerequisites
- **.NET 8.0 SDK** - Desktop & Mobile
- **.NET 9.0 SDK** - Server
- **Visual Studio 2022** - 17.8+ with MAUI workload
- **Android SDK** - API 34+ for mobile development

### Clone & Build

```bash
# Clone repository
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck

# Build desktop
dotnet build src/FocusDock.App/FocusDock.App.csproj

# Build mobile
dotnet build src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -f net9.0-android

# Build server
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj
```

### Run Locally

```bash
# Desktop (Windows only)
dotnet run --project src/FocusDock.App/FocusDock.App.csproj

# Server (cross-platform)
cd src/FocusDeck.Server
dotnet run

# Mobile (requires Android emulator or device)
dotnet build src/FocusDeck.Mobile -t:Run -f net9.0-android
```

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/FocusDeck.Mobile.Tests/FocusDeck.Mobile.Tests.csproj
```

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 🔒 Security

⚠️ **Important Security Notes**:

- The server has **no authentication** by default - suitable for private networks only
- For public deployment, use HTTPS (see [SERVER_SETUP.md](SERVER_SETUP.md))
- Configure a firewall to restrict access
- Keep your server updated regularly

### Recommended for Production:
1. Enable HTTPS with Let's Encrypt
2. Set up Nginx reverse proxy
3. Configure firewall rules
4. Add authentication (planned feature)
5. Regular backups

---

## 📝 License

[Insert your license here]

---

## 🙏 Acknowledgments

- **Home Assistant** - Inspiration for client-server architecture
- **.NET MAUI Team** - Cross-platform framework
- **Community Contributors** - Feature requests and feedback

---

## 📞 Support

- **Documentation**: [docs/](docs/)
- **Issues**: [GitHub Issues](https://github.com/dertder25t-png/FocusDeck/issues)
- **Discussions**: [GitHub Discussions](https://github.com/dertder25t-png/FocusDeck/discussions)

---

## 🗺️ Roadmap

### Phase 7 (Planned)
- 🔐 Authentication & user accounts
- 💾 PostgreSQL/MySQL database support
- 🔄 Real-time sync with SignalR
- 📧 Email notifications
- 🌙 Dark mode for web UI

### Phase 8 (Future)
- 🍎 iOS support (pending MAUI maturity)
- 🪟 Windows Store release
- 📱 Google Play release
- 🏆 Gamification & achievements
- 👥 Multi-user support

See [VISION_ROADMAP.md](VISION_ROADMAP.md) for detailed plans.

---

## 📊 Statistics

- **Languages**: C# 85%, XAML 10%, JavaScript 3%, CSS 2%
- **Lines of Code**: ~50,000+
- **Build Status**: ✅ All projects compile with 0 errors
- **Platforms**: Windows 10+, Android 8+, Linux (Ubuntu/Debian)

---

Made with ❤️ for productivity enthusiasts

