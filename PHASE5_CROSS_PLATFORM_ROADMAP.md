# 🚀 Phase 5: Cross-Platform Architecture & Enhanced Study Tools
**Strategic Goal:** Build Phase 5 features while architecting for mobile/web deployment at Stage 6

---

## 📋 Architecture First Principle

### Current State (Phase 4)
```
FocusDeck (WPF Windows Desktop)
├─ FocusDock.System (Win32 Interop) - WINDOWS ONLY ⚠️
├─ FocusDock.Core (Services, Business Logic) - SHARED ✓
├─ FocusDock.App (UI/XAML) - WINDOWS ONLY ⚠️
└─ FocusDock.Data (Models) - SHARED ✓
```

### Phase 5+ Target (Cross-Platform Ready)
```
FocusDeck.Services (Pure .NET 8) - SHARED EVERYWHERE
├─ Study Services (SessionMgmt, Analytics, Planning)
├─ Calendar/Task Services (Synchronization)
├─ API Providers (Google, Canvas, external)
└─ Data Layer (Models, Persistence, JSON)

FocusDeck.Desktop (WPF) - Windows
├─ UI Layer (XAML, Windows-specific)
└─ Platform Services (Win32 interop)

FocusDeck.Mobile (MAUI) - iOS/Android [Phase 6]
├─ UI Layer (XAML/Mobile-specific)
└─ Platform Services (iOS/Android interop)

FocusDeck.Web (Blazor/ASP.NET) - Web [Phase 6]
├─ UI Layer (Razor components)
├─ API Server (REST/gRPC)
└─ Database (PostgreSQL on Linux VM)

FocusDeck.API (ASP.NET Core) - Backend Server [Phase 6]
├─ REST API (Endpoints)
├─ Database Layer (EF Core)
├─ Authentication (JWT)
└─ Data Sync (Conflict resolution)
```

---

## 🎯 Phase 5a-5d: Enhanced Study Tools (WITH CROSS-PLATFORM PREP)

### Phase 5a: Refactor for Shared Architecture
**Duration:** 2 weeks | **Effort:** 40 hours | **Priority:** CRITICAL

#### Tasks:
1. **Create FocusDeck.Shared NuGet Package**
   - Move all models from FocusDock.Data → FocusDeck.Shared
   - Move all services (non-UI) → FocusDeck.Shared
   - Keep only UI and platform-specific code in per-platform projects
   - Remove circular dependencies

2. **Create FocusDeck.Services (New Library)**
   - Core business logic (StudySessionService, AnalyticsService, etc.)
   - No dependencies on WPF or Windows APIs
   - Pure .NET interfaces
   - Supports: Desktop, Mobile, Web, CLI

3. **Create FocusDeck.Platform Abstraction Layer**
   ```csharp
   // Services/IPlatformService.cs
   public interface IPlatformService
   {
       Task<string> GetUserDataPath();
       Task<bool> RequestNotificationPermission();
       Task SendNotification(string title, string message);
       Task<(int width, int height)> GetScreenSize();
       Task LaunchUrl(string url);
   }
   
   // Windows implementation
   // Mobile implementation [Phase 6]
   // Web implementation [Phase 6]
   ```

4. **Refactor Existing Services**
   - StudyPlanService → depends only on interfaces
   - StudySessionService → pure business logic
   - AnalyticsService → platform-agnostic
   - Remove direct Win32 dependencies

5. **Create Dependency Injection Container**
   ```csharp
   // Services/ServiceContainer.cs
   public static class ServiceConfiguration
   {
       public static IServiceCollection AddFocusDeckServices(
           this IServiceCollection services,
           PlatformType platform)
       {
           services.AddSingleton<IStudySessionService, StudySessionService>();
           services.AddSingleton<IAnalyticsService, AnalyticsService>();
           
           return services.AddPlatformServices(platform);
       }
   }
   ```

#### Deliverables:
- ✅ FocusDeck.Shared NuGet package (40% of Phase 5 codebase)
- ✅ No platform-specific code in Shared
- ✅ All services injectable
- ✅ 0 build errors
- ✅ Desktop app still works 100%

---

### Phase 5b: Voice Notes & Transcription (CROSS-PLATFORM READY)
**Duration:** 2 weeks | **Effort:** 40 hours | **Priority:** HIGH

#### Features:
1. **Audio Recording Interface**
   ```csharp
   public interface IAudioRecordingService
   {
       Task<string> StartRecording();
       Task<AudioFile> StopRecording();
       Task<string> TranscribeAudio(AudioFile file);
       Task<List<AudioNote>> GetNotes(DateTime date);
   }
   
   // Windows: Use NAudio
   // Mobile: Platform audio APIs
   // Web: WebAudio API
   ```

2. **Voice-to-Text Transcription**
   - **Desktop:** Windows Speech Recognition (built-in, free)
   - **Mobile:** Native speech APIs (iOS/Android)
   - **Web:** Web Speech API
   - Fallback: Whisper API (OpenAI) for accuracy

3. **Audio Note Storage**
   - Store audio files in `%APPDATA%/FocusDeck/audio/`
   - Metadata in JSON: `notes.json`
   - Structure for future sync to cloud

4. **Study Session Audio Integration**
   ```csharp
   // In StudySessionWindow
   public async Task RecordSessionNotes()
   {
       var audioNote = await _audioRecordingService.StartRecording();
       var transcription = await _audioRecordingService.TranscribeAudio(audioNote);
       CurrentSession.Notes += $"\n[Audio Note] {transcription}";
   }
   ```

#### Deliverables:
- ✅ IAudioRecordingService interface
- ✅ Windows implementation (NAudio + Windows Speech)
- ✅ StudySessionWindow audio button
- ✅ Audio files saved locally
- ✅ Ready for mobile implementation [Phase 6]

---

### Phase 5c: AI Study Recommendations (API-READY)
**Duration:** 2 weeks | **Effort:** 40 hours | **Priority:** HIGH

#### Architecture:
```
Client-Side (Desktop/Mobile/Web)
    ↓ (HTTP POST)
Backend API Server (Phase 6)
    ↓
LLM Provider (OpenAI/Anthropic/Local)
    ↓ (JSON Response)
Client receives recommendations
```

#### Features:
1. **Session Analysis**
   - Extract study pattern from session logs
   - Calculate optimal study times
   - Identify weak subjects

2. **Recommendation Engine**
   ```csharp
   public interface IRecommendationService
   {
       Task<StudyRecommendation> GetSessionRecommendations(
           List<StudySession> sessions,
           TimeSpan timeAvailable);
       
       Task<LearningPath> GenerateStudyPath(
           string subject,
           DateTime deadline);
       
       Task<BreakActivity> SuggestBreakActivity(
           int sessionNumber,
           StudyEffectiveness effectiveness);
   }
   ```

3. **Local vs. Server Processing**
   - Desktop: Can use local LLM (Ollama, llama.cpp)
   - Mobile: Query backend server
   - Web: Backend generates recommendations

#### Deliverables:
- ✅ IRecommendationService interface
- ✅ Desktop local implementation
- ✅ API stub for Phase 6
- ✅ Study recommendation UI window
- ✅ Ready for backend implementation

---

### Phase 5d: Focus Music & Break Activities (WITH REMOTE PLAYBACK)
**Duration:** 2 weeks | **Effort:** 40 hours | **Priority:** MEDIUM

#### Architecture:
```
Music Sources:
├─ Local Files (mp3, wav, flac)
├─ YouTube Music API
├─ Spotify API
└─ Focus@Will API

Break Activities:
├─ Local (exercises, stretches)
├─ Online (YouTube workout videos)
└─ External APIs (pomodoro trackers)
```

#### Features:
1. **Audio Playback Service**
   ```csharp
   public interface IAudioPlaybackService
   {
       Task PlayFocusMusic(MusicSource source);
       Task PauseMusic();
       Task SetVolume(int percentage);
       Task PlayAmbientSound(AmbientType type);
       // AmbientType: Rain, Forest, Ocean, Coffee Shop, etc.
   }
   ```

2. **Break Activity Suggestions**
   ```csharp
   public interface IBreakActivityService
   {
       Task<BreakActivity> SuggestActivity(
           int sessionNumber,
           int minutesAvailable);
       
       Task PlayGuidedBreakActivity(BreakActivity activity);
   }
   ```

3. **Music Integrations**
   - **Spotify:** OAuth2, playback control
   - **YouTube:** Search + embedded player
   - **Local:** Mp3 player with playlists
   - **Ambient:** Lofi Girl, Focus@Will, Spotify playlists

4. **Statistics Integration**
   ```csharp
   // Track:
   // - Most played music during sessions
   // - Most effective ambient sounds
   // - Break activity engagement
   ```

#### Deliverables:
- ✅ IAudioPlaybackService interface
- ✅ Spotify OAuth implementation
- ✅ Local music player
- ✅ Break activity UI
- ✅ Statistics tracking

---

## 🔧 Technical Implementation Strategy

### Database Structure (Future-Proof)
```json
{
  "studySessions": [
    {
      "id": "uuid",
      "subject": "string",
      "startTime": "ISO8601",
      "endTime": "ISO8601",
      "durationMinutes": "number",
      "effectiveness": "1-5",
      "audioNoteId": "uuid",
      "musicId": "string",
      "breakActivities": ["string"],
      "notes": "string",
      "syncedToServer": false,
      "lastModified": "ISO8601"
    }
  ],
  "audioNotes": [
    {
      "id": "uuid",
      "sessionId": "uuid",
      "fileName": "string",
      "transcription": "string",
      "duration": "seconds",
      "createdAt": "ISO8601"
    }
  ],
  "recommendations": [
    {
      "id": "uuid",
      "generatedAt": "ISO8601",
      "type": "study_pattern|break_activity|music|learning_path",
      "data": "json",
      "appliedCount": "number"
    }
  ]
}
```

### File Structure for Phase 6 Transition
```
FocusDeck/
├─ docs/
│  ├─ API_SPECIFICATION.md (Phase 6 API endpoints)
│  ├─ DATABASE_SCHEMA.md (PostgreSQL schema)
│  ├─ DEPLOYMENT_GUIDE.md (Linux VM setup)
│  └─ SYNC_PROTOCOL.md (Client↔Server sync)
├─ src/
│  ├─ FocusDeck.Shared/ (Cross-platform)
│  ├─ FocusDeck.Services/ (Core logic)
│  ├─ FocusDeck.Desktop/ (WPF)
│  ├─ FocusDeck.Mobile/ (Empty, Phase 6)
│  ├─ FocusDeck.Web/ (Empty, Phase 6)
│  └─ FocusDeck.API/ (Empty, Phase 6)
├─ .github/
│  ├─ workflows/
│  │  ├─ build-desktop.yml
│  │  ├─ build-mobile.yml
│  │  └─ build-api.yml
│  └─ ISSUE_TEMPLATE/
├─ docker/
│  ├─ Dockerfile (For Linux VM)
│  ├─ docker-compose.yml
│  └─ nginx.conf
├─ .gitignore (Exclude audio files, local DB)
└─ README.md (Multi-platform overview)
```

### Performance Optimization Points
1. **Audio Processing:** Async transcription, don't block UI
2. **Analytics:** Lazy-load 30-day data, cache weekly stats
3. **Music Playback:** Stream instead of download, buffer ahead
4. **Recommendations:** Batch process overnight, cache results
5. **Sync Strategy:** Queue offline changes, merge on reconnect

### Code Quality Standards
- **Unit Tests:** 80% coverage for services
- **Integration Tests:** Test cross-platform compatibility
- **Performance Benchmarks:** Track response times
- **Code Style:** .NET coding guidelines, SonarQube analysis
- **Documentation:** Every public API documented

---

## 📅 Phase 5 Implementation Schedule

| Week | 5a | 5b | 5c | 5d | Status |
|------|----|----|----|----|--------|
| 1    | Shared ✓ | | | | Setup |
| 2    | DI ✓ | | | | Architecture |
| 3    | | Audio ✓ | | | Voice Notes |
| 4    | | Recording ✓ | | | Recording |
| 5    | | | AI ✓ | | Recommendations |
| 6    | | | API ✓ | | API Design |
| 7    | | | | Music ✓ | Music Integration |
| 8    | | | | Breaks ✓ | Break Activities |

---

## ✅ Phase 5 Completion Criteria

- ✅ FocusDeck.Shared library created (0 platform deps)
- ✅ All services have interfaces
- ✅ Dependency injection configured
- ✅ Voice notes recording & transcription working
- ✅ AI recommendations engine functional
- ✅ Music playback integrated
- ✅ Break activity system operational
- ✅ 0 build errors, 0 warnings
- ✅ Performance benchmarks documented
- ✅ Code ready for mobile/web adaptation
- ✅ GitHub-ready code structure
- ✅ Deployment documentation drafted

---

## 🎯 Success Metrics

| Metric | Target | Phase 5 Checkpoint |
|--------|--------|-------------------|
| Build Time | < 5s | < 3s ✓ |
| App Startup | < 2s | < 1.5s ✓ |
| Session Timer Update | 60fps | 60fps ✓ |
| Audio Transcription | < 5s | < 3s (local) |
| Recommendation API | < 1s | < 500ms |
| Code Duplication | < 5% | Platform-agnostic |
| Test Coverage | 80% | 85% |

---

## 🚀 Phase 5→6 Transition Checklist

Once Phase 5 complete:
- [ ] Extract API endpoints from service interfaces
- [ ] Design PostgreSQL schema from JSON models
- [ ] Create ASP.NET Core API project
- [ ] Set up authentication (JWT)
- [ ] Implement data sync protocol
- [ ] Dockerize backend for Linux VM
- [ ] Create mobile project (MAUI)
- [ ] Create web project (Blazor)
- [ ] Set up GitHub Actions CI/CD

---

## 💡 Key Principles for Phase 5

1. **Think Distributed:** Every component should work offline or online
2. **Data Sync:** All data timestamped for future merge conflicts
3. **No Lock-In:** Use APIs, not proprietary formats
4. **Performance First:** Profile before optimizing, measure after
5. **Testability:** Mock external services, test core logic
6. **Deployability:** Docker-ready from day 1
7. **Documentation:** API specs, deployment guides, setup instructions

This structure ensures that Phase 6 (mobile/web) is purely a matter of adding new UI layers on top of proven, cross-platform services. 🎉
