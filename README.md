# FocusDeck + Jarvis 🎯

**AI-first productivity suite with cloud synchronization for desktop, mobile, and web**

> Last Updated: November 2025

FocusDeck + Jarvis is a cross-platform productivity suite that combines window management, study timers, session tracking, and an AI-first feature set to enhance your workflow. Focus on what matters while Jarvis handles the rest.

---

## 🚀 Quick Start

### 🌐 Web Application (Browser-Based SaaS)

Access FocusDeck through your web browser. The new FocusDeck + Jarvis execution plan standardizes the SPA at the root path:

```
https://your-focusdeck-domain.com/
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

Deploy your own sync server in **one command**:

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | sudo bash
```

The script automatically installs and configures:
- ✅ .NET 9.0 SDK
- ✅ Git and dependencies
- ✅ FocusDeck application
- ✅ Systemd service (auto-start enabled)
- ✅ SQLite or PostgreSQL database

📖 **Installation Guide**: [LINUX_INSTALL.md](LINUX_INSTALL.md)
📖 **Advanced Configuration**: [docs/CLOUDFLARE_DEPLOYMENT.md](docs/CLOUDFLARE_DEPLOYMENT.md)

**Requirements**: Ubuntu 20.04+ / Debian 11+, 512MB RAM, 1GB disk space

---

## 🌟 What is FocusDeck + Jarvis?

FocusDeck + Jarvis is an AI-first productivity suite designed to automate and enhance your workflow. Key features include:

- **🤖 Jarvis Contextual Learning Loop**: An adaptive assistant that learns from your context while preserving privacy.
- **🧠 AI Memory Vault**: Capture and organize your research, AI chats, and tab context into a personal knowledge notebook.
- **📅 AI Study Planner**: Auto-generate study plans from calendar assignments.
- **🪟 Window Management**: Auto-organize apps into layouts, save workspaces.
- **⏱️ Study Timers**: Configurable Pomodoro sessions with tracking.
- **☁️ Cloud Sync**: Seamlessly sync across Windows, Android, and web.

---

## 📊 Project Status

The project is currently in **Phase 1: SaaS Foundation + Auth UI + URL Fixes**. See the [FocusDeck + Jarvis Execution Roadmap](docs/FocusDeck_Jarvis_Execution_Roadmap.md) for detailed plans.

---

## 🏗️ Architecture

### Server (ASP.NET Core)
- **.NET 9.0** - Latest performance improvements
- **Layered Architecture** - Domain, Persistence, Contracts, SharedKernel
- **Multi-Tenancy** - Tenant-scoped data and services
- **JWT Authentication** - Access & refresh tokens
- **OpenTelemetry** - Distributed tracing and metrics
- **SignalR** - Real-time notifications hub
- **PostgreSQL/SQLite** - Dual database support

### Web UI (Vite/React)
- **React + TypeScript** - Modern web application framework
- **Vite** - Fast build tooling
- **Tailwind CSS** - Utility-first CSS framework

### Desktop (WPF)
- **.NET 9.0** - Windows Presentation Foundation
- **Win32 P/Invoke** - Native window management
- **RESTful API Client** - Server communication

### Mobile (MAUI)
- **.NET MAUI** - Cross-platform framework
- **SQLite-net-pcl** - Local database
- **MVVM Pattern** - Clean architecture

---

## 📚 Documentation

- [FocusDeck + Jarvis Execution Roadmap](docs/FocusDeck_Jarvis_Execution_Roadmap.md) - The single source of truth for the project's roadmap.
- [LINUX_INSTALL.md](LINUX_INSTALL.md) - Server installation guide.
- [CLOUDFLARE_DEPLOYMENT.md](docs/CLOUDFLARE_DEPLOYMENT.md) - Advanced server configuration.
- [DEVELOPMENT.md](DEVELOPMENT.md) - Developer guide.

---

## 🛠️ Development Setup

### Prerequisites
- **.NET 9.0 SDK**
- **Node.js** (for WebApp development)
- **Visual Studio 2022** - 17.8+ with MAUI workload (for mobile/desktop)

### Clone & Build

```bash
# Clone repository
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck

# Build server and webapp
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj
```

### Run Locally

```bash
# Server and WebApp
cd src/FocusDeck.Server
dotnet run
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

## 📝 License

[Insert your license here]
