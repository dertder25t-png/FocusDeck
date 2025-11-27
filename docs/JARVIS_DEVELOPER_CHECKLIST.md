#  JARVIS Implementation - Developer Checklist

**Status:** Phase 1 Ready to Build  
**Updated:** November 5, 2025  

---

##  Pre-Development Checklist

### Documentation (DONE - YOU HAVE ALL THESE)
- [x] JARVIS_QUICK_START.md
- [x] JARVIS_IMPLEMENTATION_ROADMAP.md
- [x] JARVIS_PHASE1_DETAILED.md
- [x] JARVIS_INTEGRATION_WITH_FOCUSDECK.md
- [x] JARVIS_DOCUMENTATION_INDEX.md
- [x] JARVIS_ROADMAP_COMPLETE.md (in root)

### Understanding
- [ ] Read JARVIS_QUICK_START.md (10 min)
- [ ] Read JARVIS_PHASE1_DETAILED.md (60 min)
- [ ] Understand Task 1.1-1.4
- [ ] Review existing FocusDeck service patterns
- [ ] Check existing NotificationsHub SignalR setup

### Environment Setup
- [ ] Git pull latest from develop branch
- [ ] dotnet restore (ensure all packages ready)
- [ ] Review FocusDeck.sln structure
- [ ] Verify PostgreSQL/SQLite connection
- [ ] Test existing build: dotnet build (should pass)

---

##  Phase 1: Activity Detection (Weeks 1-4)

### Week 1: Interface Design

#### Task 1.1: IActivityDetectionService Interface
- [ ] File created: src/FocusDeck.Services/Activity/IActivityDetectionService.cs
- [ ] IActivityDetectionService interface defined
- [ ] ActivityState class defined
- [ ] FocusedApplication class defined
- [ ] ContextItem class defined
- [ ] Code compiles without errors
- [ ] Unit tests written (mock service)
- [ ] Tests pass
- [ ] PR submitted & approved
- [ ] Merged to develop

#### Task 1.2: Base ActivityDetectionService Class
- [ ] File created: src/FocusDeck.Services/Activity/ActivityDetectionService.cs
- [ ] Abstract class inherits IActivityDetectionService
- [ ] Subject<ActivityState> property defined
- [ ] GetCurrentActivityAsync implemented (base logic)
- [ ] IsIdleAsync implemented
- [ ] RecordActivity method implemented
- [ ] Platform-specific abstract methods defined
- [ ] IObservable<ActivityState> works correctly
- [ ] Unit tests pass (>80% coverage)
- [ ] PR submitted & approved
- [ ] Merged to develop

**End of Week 1 Status:**
- [ ] Interfaces + base class committed
- [ ] 0 compilation errors
- [ ] Ready for platform implementations

---

### Week 2: Windows Implementation

#### Task 2.1: WindowsActivityDetectionService
- [ ] File created: src/FocusDeck.Desktop/Services/WindowsActivityDetectionService.cs
- [ ] Class inherits ActivityDetectionService
- [ ] Platform check: #if NET8_0_WINDOWS
- [ ] P/Invoke declarations added (SetWinEventHook, GetForegroundWindow, etc.)
- [ ] Hook initialization in constructor
- [ ] Hook cleanup in destructor
- [ ] GetFocusedApplicationInternalAsync implemented
- [ ] Window title extraction working
- [ ] Process name extraction working
- [ ] App classification logic working (ClassifyApplication method)
- [ ] GetActivityIntensityInternalAsync stubbed (can enhance later)

#### Task 2.2: Windows Testing & Performance
- [ ] Integration test: Windows hook works
- [ ] Test: App switching detected correctly (>95% accuracy)
- [ ] Test: Process name extraction works
- [ ] Performance test: CPU usage <5% over 30 mins
- [ ] Performance test: Event loop doesn't block UI
- [ ] Manual testing on your Windows machine
- [ ] Tested with 10+ different apps
- [ ] No false negatives (every window switch detected)

**End of Week 2 Status:**
- [ ] Windows implementation complete & tested
- [ ] Ready for Linux implementation
- [ ] All tests pass

---

### Week 3: Linux & Mobile Implementation

#### Task 3.1: LinuxActivityDetectionService
- [ ] File created: src/FocusDeck.Server/Services/Activity/LinuxActivityDetectionService.cs
- [ ] Class inherits ActivityDetectionService
- [ ] Platform check: #if LINUX
- [ ] IProcessService injected (for executing wmctrl/xdotool)
- [ ] GetFocusedApplicationInternalAsync implemented
- [ ] wmctrl -l parsing implemented
- [ ] Window title extraction from wmctrl
- [ ] xdotool integration for window details
- [ ] GetActivityIntensityInternalAsync stubbed
- [ ] Error handling (wmctrl not installed, etc.)

#### Task 3.2: Mobile Activity Detection (MAUI)
- [ ] File created: src/FocusDeck.Mobile/Services/MobileActivityDetectionService.cs
- [ ] Class inherits ActivityDetectionService
- [ ] Platform check: #if NET8_0_ANDROID
- [ ] Accelerometer integration
- [ ] Gyroscope integration
- [ ] Sensor event handlers implemented
- [ ] Motion counting logic
- [ ] GetFocusedApplicationInternalAsync returns "FocusDeck"
- [ ] GetActivityIntensityInternalAsync uses motion count
- [ ] Sensor initialization in constructor

#### Testing (All Platforms)
- [ ] Linux test: Window switching detected
- [ ] Linux test: wmctrl parsing works
- [ ] Linux test: Error handling (wmctrl not found)
- [ ] Mobile test: Accelerometer detected
- [ ] Mobile test: Motion intensity calculated
- [ ] Mobile test: Sensor cleanup on app close
- [ ] Cross-platform unit tests pass

**End of Week 3 Status:**
- [ ] All three platform implementations complete
- [ ] Each platform tested on actual device/OS
- [ ] Ready for context aggregation

---

### Week 4: Context Aggregation & Integration

#### Task 4.1: IContextAggregationService
- [ ] Interface created: src/FocusDeck.Services/Activity/IContextAggregationService.cs
- [ ] GetAggregatedContextAsync method defined
- [ ] ContextChanged IObservable property defined
- [ ] StudentContext DTO defined (with all required fields)
- [ ] Implementation: ContextAggregationService.cs

#### Task 4.2: Database Schema & Migrations
- [ ] StudentContext entity created: src/FocusDeck.Domain/Entities/JARVIS/StudentContext.cs
- [ ] StudentContextConfiguration created: src/FocusDeck.Persistence/Configurations/JARVIS/StudentContextConfiguration.cs
- [ ] EF Core migration created: dotnet ef migrations add AddStudentContext
- [ ] Migration applied to database
- [ ] Verify tables created in PostgreSQL
- [ ] Verify schema matches entity definition
- [ ] Add indexes for StudentId and Timestamp

#### Task 4.3: DI Registration
- [ ] Program.cs updated with platform-specific service registration
- [ ] Windows: RegisterActivationService added
- [ ] Linux: LinuxActivityDetectionService registered
- [ ] Mobile: MobileActivityDetectionService registered
- [ ] IContextAggregationService registered as Scoped
- [ ] Application builds without errors
- [ ] DI container can resolve all services

#### Task 4.4: SignalR Integration
- [ ] NotificationsHub updated with new methods
- [ ] INotificationClient interface extended with new hub methods
- [ ] ContextAggregationService calls HubContext.Clients.User()
- [ ] Real-time context broadcasts working
- [ ] Test: Context changes broadcast via SignalR
- [ ] Test: Multiple clients receive updates

#### Task 4.5: End-to-End Testing
- [ ] Integration test: Activity detection  Context aggregation  SignalR
- [ ] Test: Context aggregation latency <100ms
- [ ] Test: All platforms report consistent context
- [ ] Test: SignalR delivery within 500ms
- [ ] Test: No memory leaks (run 1 hour, monitor RAM)
- [ ] Performance test: CPU usage <5% sustained
- [ ] Load test: 10 concurrent users, all updating context

**End of Week 4 Status:**
- [ ] Phase 1 COMPLETE 
- [ ] All 4 tasks done
- [ ] All tests passing
- [ ] Ready to demo to stakeholders
- [ ] Ready to move to Phase 2

---

##  Quality Gates (Before Moving to Phase 2)

### Code Quality
- [ ] Code compiles without warnings
- [ ] Code follows FocusDeck conventions (see existing code)
- [ ] All public methods documented (XML comments)
- [ ] No dead code or commented-out lines
- [ ] Tests have >80% code coverage
- [ ] All tests pass (0 failures)

### Performance
- [ ] Activity detection CPU overhead <5%
- [ ] Context aggregation latency <100ms
- [ ] Memory usage stable (no leaks over 1 hour)
- [ ] No UI blocking on any platform

### Functionality
- [ ] Windows: App detection >95% accurate
- [ ] Linux: App detection >95% accurate
- [ ] Mobile: Motion detection working
- [ ] Context state broadcasts in real-time
- [ ] SignalR delivery reliable

### Cross-Platform
- [ ] Windows builds & runs
- [ ] Linux builds & runs
- [ ] Mobile builds & runs
- [ ] All platforms work independently
- [ ] All platforms can be deployed separately

---

##  Reference Materials (Keep Open While Coding)

### During Phase 1

- [x] JARVIS_PHASE1_DETAILED.md (week-by-week tasks)
- [x] JARVIS_INTEGRATION_WITH_FOCUSDECK.md (schema, DI, folders)
- [ ] Open existing FocusDeck code:
  - [ ] src/FocusDeck.Services/ExistingService.cs (for patterns)
  - [ ] src/FocusDeck.Persistence/Configurations/ExistingConfiguration.cs
  - [ ] src/FocusDeck.Server/Hubs/NotificationsHub.cs
  - [ ] src/FocusDeck.Desktop/Services/ExistingDesktopService.cs
  - [ ] src/FocusDeck.Mobile/Services/ExistingMobileService.cs

---

##  Daily Developer Workflow

### Start of Day
- [ ] Git pull latest
- [ ] dotnet build (verify no breaks)
- [ ] Open relevant JARVIS doc
- [ ] Know your day's task (from PHASE1_DETAILED.md)

### During Day
- [ ] Code 1 task (use code snippets from docs)
- [ ] Test as you code
- [ ] Reference INTEGRATION guide for patterns
- [ ] Keep tests passing

### End of Day
- [ ] Tests pass (dotnet test)
- [ ] Commit work
- [ ] Note what's done in commit message
- [ ] Document any blockers

### Weekly
- [ ] Code review with team
- [ ] Merge PRs
- [ ] Update progress
- [ ] Plan next week

---

##  Troubleshooting Quick Reference

### "How do I register a new service in DI?"
 See JARVIS_INTEGRATION_WITH_FOCUSDECK.md  DI Registration section

### "Where should this file go?"
 See JARVIS_INTEGRATION_WITH_FOCUSDECK.md  Folder Structure section

### "What's the database schema?"
 See JARVIS_INTEGRATION_WITH_FOCUSDECK.md  Database Schema section

### "How do I integrate with existing Canvas service?"
 Look at existing ICanvasService usage in FocusDeck codebase

### "How do I broadcast via SignalR?"
 Look at NotificationsHub.cs existing methods

### "How do I test this?"
 See JARVIS_PHASE1_DETAILED.md  Week X  Tests section

---

##  Success Milestones

- [ ] **Day 1:** Documentation read, branch created
- [ ] **Day 2-3:** Task 1.1 complete (interface + base class)
- [ ] **Day 4-5:** Task 1.2 complete
- [ ] **Week 1 End:** PR submitted, Week 1 DONE 
- [ ] **Week 2 End:** Windows implementation done
- [ ] **Week 3 End:** Linux + Mobile done
- [ ] **Week 4 End:** Context aggregation + integration done
- [ ] **Week 4 End:** Phase 1 COMPLETE 

---

##  Commit Message Template

`
feat: [Phase 1] Task 1.1 - IActivityDetectionService interface

- Defined IActivityDetectionService interface
- Created ActivityState, FocusedApplication, ContextItem classes
- Implemented base ActivityDetectionService abstract class
- Added unit tests
- Tests: 12/12 passing

Related: JARVIS Phase 1 - Activity Detection
`

---

##  You're Ready!

All documentation is in place. All code snippets are provided. You have everything you need to build Phase 1.

**Next Step:** Read JARVIS_QUICK_START.md (10 min)

**Then:** Read JARVIS_PHASE1_DETAILED.md Week 1 (30 min)

**Then:** Start coding Task 1.1

Let's build JARVIS! 

---

**Questions?** Open JARVIS_DOCUMENTATION_INDEX.md
