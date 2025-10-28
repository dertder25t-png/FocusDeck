# 🚀 Deployment & GitHub Strategy
**Goal:** Prepare infrastructure for Phase 6 (GitHub + Linux VM deployment)

---

## Phase 5 Preparation for GitHub Upload

### 1. Code Organization (Already Planning)
```
FocusDeck/
├─ .gitignore (Ignore binaries, user data)
├─ .github/
│  ├─ workflows/
│  │  ├─ build-desktop.yml (CI/CD for desktop)
│  │  ├─ build-mobile.yml (CI/CD for mobile - Phase 6)
│  │  └─ build-api.yml (CI/CD for API - Phase 6)
│  └─ ISSUE_TEMPLATE/
│     ├─ bug_report.md
│     └─ feature_request.md
├─ docs/
│  ├─ ARCHITECTURE.md (Cross-platform design)
│  ├─ API_SPECIFICATION.md (Phase 6 endpoints)
│  ├─ DEPLOYMENT.md (Linux VM setup)
│  ├─ CONTRIBUTING.md (Dev guidelines)
│  └─ DATABASE_SCHEMA.md (PostgreSQL schema)
├─ docker/
│  ├─ Dockerfile (For API container)
│  ├─ docker-compose.yml (Full stack)
│  └─ nginx.conf (Reverse proxy config)
├─ src/
│  ├─ FocusDeck.Shared/
│  ├─ FocusDeck.Services/
│  ├─ FocusDeck.Core/
│  ├─ FocusDeck.Desktop/
│  ├─ FocusDeck.Mobile/ (Empty Phase 6)
│  ├─ FocusDeck.API/ (Empty Phase 6)
│  └─ FocusDeck.Web/ (Empty Phase 6)
├─ tests/
│  ├─ FocusDeck.Services.Tests/
│  └─ FocusDeck.API.Tests/ (Phase 6)
├─ README.md (Multi-platform overview)
├─ LICENSE (MIT)
├─ .gitattributes (Line endings)
└─ FocusDeck.sln
```

### 2. .gitignore Template
```gitignore
# Build results
bin/
obj/
dist/
publish/

# User data (NEVER commit)
/Documents/FocusDeck/
/AppData/
*.db
*.sqlite
local.json
appsettings.local.json

# Audio files (too large)
audio/
recordings/

# IDE
.vs/
.vscode/
*.user
*.opendb
.DS_Store

# OS
Thumbs.db
*.log

# Dependencies
node_modules/
.npm

# Environment
.env
.env.local
```

### 3. GitHub Repository Setup

#### README.md Structure
```markdown
# FocusDeck 🎓
Study productivity platform with focus timer, task management, and AI recommendations.

## 🌍 Platforms
- 🖥️ **Windows Desktop** (Phase 1-4: Complete)
- 📱 **iOS/Android** (Phase 6: Planned)
- 🌐 **Web** (Phase 6: Planned)
- ⚙️ **Linux Server** (Phase 6: Planned)

## 🚀 Quick Start

### Desktop
```bash
git clone https://github.com/yourusername/FocusDeck.git
cd FocusDeck
dotnet build
dotnet run --project src/FocusDeck.Desktop/FocusDeck.Desktop.csproj
```

### Server (Phase 6)
```bash
docker-compose up -d
# API running on http://localhost:5000
```

## 📚 Documentation
- [Architecture](docs/ARCHITECTURE.md)
- [Setup Guide](docs/SETUP.md)
- [Deployment](docs/DEPLOYMENT.md)
- [API Spec](docs/API_SPECIFICATION.md)

## 🤝 Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md)

## 📄 License
MIT License
```

#### CONTRIBUTING.md
```markdown
# Contributing to FocusDeck

## Development Setup
1. Clone repo
2. Open in VS Code
3. Install C# extension
4. `dotnet restore && dotnet build`

## Code Style
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use SonarQube for analysis: `dotnet sonarscanner begin`
- Prefix interfaces with `I`: `IStudyService`
- Async methods end with `Async`: `GetSessionAsync()`

## Testing
```bash
dotnet test
```

## Pull Request Process
1. Fork the repo
2. Create feature branch: `git checkout -b feature/my-feature`
3. Commit with clear messages
4. Push and create PR
5. Request review from maintainer
6. Squash and merge when approved
```

---

## Phase 6: API & Backend Setup

### Architecture (Ready to implement)
```
┌─────────────────────────────────────────┐
│         Client Applications             │
├─────────┬──────────────┬────────────────┤
│ Desktop │    Mobile    │      Web       │
│ (.NET)  │   (MAUI)     │   (Blazor)     │
└────┬────┴──────┬───────┴────┬───────────┘
     │           │            │
     └───────────┴────────────┘
           ↓ (REST/gRPC)
┌──────────────────────────────┐
│   ASP.NET Core API Server    │
├──────────────────────────────┤
│ Authentication (JWT)         │
│ Data Sync Engine             │
│ Recommendation Engine        │
│ File Upload Handler          │
└──────┬───────────────────────┘
       ↓
┌──────────────────────────────┐
│   PostgreSQL Database        │
│   (On Linux VM / Proxmox)    │
├──────────────────────────────┤
│ Users                        │
│ Study Sessions (synced)      │
│ Audio Metadata               │
│ Recommendations Cache        │
│ Settings                     │
└──────────────────────────────┘
```

### PostgreSQL Schema (Preview for Phase 6)
```sql
-- Users table
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP
);

-- Study sessions (synced from desktop)
CREATE TABLE study_sessions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    subject VARCHAR(255) NOT NULL,
    start_time TIMESTAMP NOT NULL,
    end_time TIMESTAMP NOT NULL,
    duration_minutes INT,
    effectiveness INT CHECK (effectiveness >= 1 AND effectiveness <= 5),
    music_id VARCHAR(255),
    audio_note_id UUID,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_modified TIMESTAMP,
    synced_at TIMESTAMP
);

-- Audio metadata
CREATE TABLE audio_notes (
    id UUID PRIMARY KEY,
    session_id UUID NOT NULL REFERENCES study_sessions(id),
    user_id UUID NOT NULL REFERENCES users(id),
    file_path VARCHAR(255) NOT NULL,
    transcription TEXT,
    duration_seconds INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Recommendations
CREATE TABLE recommendations (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    type VARCHAR(50) NOT NULL,
    data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    applied_at TIMESTAMP
);

-- Sessions Index for fast queries
CREATE INDEX idx_sessions_user_date ON study_sessions(user_id, start_time DESC);
CREATE INDEX idx_recommendations_user ON recommendations(user_id, created_at DESC);
```

### Docker Setup for Linux VM
```dockerfile
# Dockerfile (root of project)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and projects
COPY ["FocusDeck.sln", "."]
COPY ["src/", "src/"]

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "FocusDeck.API.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "5000:5000"
    environment:
      - DATABASE_URL=postgresql://user:password@db:5432/focusdeck
      - JWT_SECRET=your-secret-key-change-this
    depends_on:
      - db
    restart: unless-stopped

  db:
    image: postgres:16
    environment:
      - POSTGRES_DB=focusdeck
      - POSTGRES_USER=focusdeck
      - POSTGRES_PASSWORD=secure-password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

  nginx:
    image: nginx:latest
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./docker/nginx.conf:/etc/nginx/nginx.conf:ro
      - /etc/letsencrypt:/etc/letsencrypt:ro
    depends_on:
      - api
    restart: unless-stopped

volumes:
  postgres_data:
```

---

## Performance Optimization Strategy

### Phase 5 Optimizations (Desktop)
1. **Lazy Loading:** Don't load 30-day analytics until tab clicked
2. **Async Everything:** No blocking UI threads
3. **Caching:** Cache weekly stats, invalidate daily
4. **Compression:** Gzip JSON files in storage

### Phase 6 Optimizations (Server)
1. **Database Indexes:** Fast date-range queries
2. **Connection Pooling:** Reuse DB connections
3. **API Response Caching:** Redis for recommendations
4. **CDN for Audio:** Store audio on S3/MinIO
5. **GraphQL (Optional):** More efficient than REST

### Speed Targets
| Operation | Target | Measurement |
|-----------|--------|-------------|
| App Startup | < 2s | Time to main window |
| Session Timer Update | 60 FPS | Smooth animation |
| Audio Transcription | < 3s | Local speech-to-text |
| Analytics Load | < 1s | 30-day stats |
| API Response | < 200ms | p50 latency |
| Sync on Connect | < 500ms | Merge conflicts |

---

## Linux VM Setup (For Phase 6)

### Proxmox VM Requirements
```
OS: Ubuntu 22.04 LTS
CPU: 2-4 vCores
RAM: 4-8 GB
Disk: 50 GB (OS + DB + Docker)
```

### Initial Setup Script
```bash
#!/bin/bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Clone FocusDeck
git clone https://github.com/yourusername/FocusDeck.git
cd FocusDeck

# Configure environment
cp .env.example .env
# Edit .env with your settings

# Start services
docker-compose up -d

# View logs
docker-compose logs -f api
```

### Let's Encrypt SSL Cert
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot certonly -d api.yourdomain.com
# Update docker-compose.yml with cert paths
docker-compose restart nginx
```

---

## Continuous Integration (GitHub Actions)

### Desktop Build Pipeline
```yaml
# .github/workflows/build-desktop.yml
name: Build Desktop

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release
      
      - name: Test
        run: dotnet test
      
      - name: Publish
        if: startsWith(github.ref, 'refs/tags/')
        run: dotnet publish -c Release -o publish/desktop
      
      - name: Upload Release
        if: startsWith(github.ref, 'refs/tags/')
        uses: softprops/action-gh-release@v1
        with:
          files: publish/desktop/**
```

---

## Git Workflow

### Branching Strategy
```
main (production)
├─ release/1.0.0 (release branch)
└─ develop (development)
   ├─ feature/phase-5a-refactor
   ├─ feature/phase-5b-voice-notes
   ├─ feature/phase-5c-ai-recommendations
   └─ feature/phase-5d-music
```

### Commit Guidelines
```
feat: Add voice notes recording
fix: Resolve audio transcription timeout
docs: Update deployment guide
refactor: Simplify study session service
test: Add analytics service tests
chore: Update dependencies
```

---

## Deployment Checklist for Phase 6

- [ ] Code pushed to GitHub (public or private)
- [ ] CI/CD workflows passing
- [ ] Docker images building successfully
- [ ] Linux VM running on Proxmox
- [ ] PostgreSQL database initialized
- [ ] API server running behind nginx
- [ ] SSL certificates configured
- [ ] Domain DNS pointing to server
- [ ] Mobile app connecting to API
- [ ] Web app connecting to API
- [ ] Data sync working bi-directionally
- [ ] Monitoring/alerts set up

---

## Success Criteria

✅ Phase 5 Complete
- All code pushed to GitHub
- Desktop app 100% functional
- Cross-platform architecture ready
- Performance optimized
- Documented thoroughly

✅ Phase 6 Ready
- Spin up Linux VM with `docker-compose up -d`
- API running and responding
- Mobile/Web apps sync data seamlessly
- All platforms working together
- Deployment automation in place

This sets you up for a smooth transition to multi-platform support! 🎉
