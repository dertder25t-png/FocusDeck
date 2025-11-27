# Merge Conflict Resolution Guide
## PR #10: feature-browser-bridge-memory-vault → feature-gemini-embeddings-pivot

**Date:** November 20, 2025  
**Status:** Manual Resolution Required

## Executive Summary

This merge introduces **9 conflicting files** due to unrelated branch histories. The primary conflict involves fundamental changes to the `AutomationTrigger` domain model, which cascades through controllers, services, and tests.

## Conflict Analysis

### Critical Conflicts (Must Resolve First)

#### 1. Domain Model Mismatch: AutomationTrigger

**Files Affected:**
- `src/FocusDeck.Persistence/AutomationDbContext.cs`
- `src/FocusDeck.Server/Services/AutomationEngine.cs`
- `src/FocusDeck.Server/Controllers/v1/Jarvis/AutomationProposalsController.cs`

**Issue:**  
The `AutomationTrigger` entity has different properties in each branch:
- `feature-gemini-embeddings-pivot`: Unknown structure (base branch)
- `feature-browser-bridge-memory-vault`: Expects `Type` and `Configuration` properties

**Build Errors:**
```
AutomationEngine.cs(80,44): error CS1061: 'AutomationTrigger' does not contain a definition for 'Type'
AutomationProposalsController.cs(84,89): error CS0117: 'AutomationTrigger' does not contain a definition for 'Type'
AutomationProposalsController.cs(84,112): error CS0117: 'AutomationTrigger' does not contain a definition for 'Configuration'
```

**Recommended Resolution:**
1. Compare `AutomationTrigger` class in both branches:
   ```bash
   git show feature-gemini-embeddings-pivot:src/FocusDeck.Domain/Entities/AutomationTrigger.cs
   git show feature-browser-bridge-memory-vault:src/FocusDeck.Domain/Entities/AutomationTrigger.cs
   ```
2. Decide on unified schema (likely from `feature-browser-bridge-memory-vault` since it's more recent)
3. Update `AutomationDbContext` with proper entity configuration
4. Create EF Core migration if schema changes

### Secondary Conflicts (Dependent on Domain Fix)

#### 2. Controller Implementations

**Files:**
- `src/FocusDeck.Server/Controllers/v1/Jarvis/AutomationProposalsController.cs`
- `src/FocusDeck.Server/Controllers/v1/RemoteController.cs`

**Suggested Resolution:**
- After fixing `AutomationTrigger`, review controller logic from both branches
- Merge endpoint implementations (likely both branches add new endpoints)
- Ensure DTO mappings match new domain model
- Preserve all new API endpoints from both branches

#### 3. SignalR Hub Changes

**File:**
- `src/FocusDeck.Server/Hubs/NotificationsHub.cs`

**Suggested Resolution:**
- Merge hub method signatures from both branches
- Update `INotificationClient` interface if needed
- Test real-time notifications after merge

#### 4. Automation Engine Logic

**File:**
- `src/FocusDeck.Server/Services/AutomationEngine.cs`

**Null Reference Error:**
```
AutomationEngine.cs(87,57): error CS8604: Possible null reference argument
```

**Suggested Resolution:**
- Add null checks or null-forgiving operator after reviewing logic
- Ensure trigger evaluation logic is compatible with new schema
- Merge any new trigger types from both branches

#### 5. Frontend Application

**File:**
- `src/FocusDeck.WebApp/src/App.tsx`

**Suggested Resolution:**
- Merge React component changes from both branches
- Ensure TypeScript types match backend DTOs
- Test UI for automation proposals and remote control features

### Test Conflicts

**Files:**
- `tests/FocusDeck.Server.Tests/FocusSessionTests.cs`
- `tests/FocusDeck.Server.Tests/ForcedLogoutPropagationTests.cs`
- `tests/FocusDeck.Server.Tests/JarvisControllerTests.cs`

**Suggested Resolution:**
- Merge test cases from both branches (additive merge)
- Update test data to match new `AutomationTrigger` schema
- Run `dotnet test` after all code conflicts resolved
- Ensure 70%+ code coverage is maintained

## Step-by-Step Resolution Process

### Phase 1: Prepare Environment
```bash
# Create resolution branch
git checkout feature-gemini-embeddings-pivot
git checkout -b merge-resolution/browser-bridge-into-gemini

# Attempt merge with unrelated histories
git merge --no-ff --allow-unrelated-histories feature-browser-bridge-memory-vault
```

### Phase 2: Resolve Domain Model
```bash
# Review AutomationTrigger differences
git diff HEAD feature-browser-bridge-memory-vault -- src/FocusDeck.Domain/Entities/

# Open conflict in editor
code src/FocusDeck.Domain/Entities/AutomationTrigger.cs

# After manual resolution, stage
git add src/FocusDeck.Domain/Entities/AutomationTrigger.cs
```

### Phase 3: Resolve Database Context
```bash
# Merge AutomationDbContext changes
code src/FocusDeck.Persistence/AutomationDbContext.cs

# Ensure entity configuration is correct
code src/FocusDeck.Persistence/Configurations/AutomationTriggerConfiguration.cs

git add src/FocusDeck.Persistence/
```

### Phase 4: Fix Controllers and Services
```bash
# Resolve each controller
code src/FocusDeck.Server/Controllers/v1/Jarvis/AutomationProposalsController.cs
code src/FocusDeck.Server/Controllers/v1/RemoteController.cs
code src/FocusDeck.Server/Services/AutomationEngine.cs
code src/FocusDeck.Server/Hubs/NotificationsHub.cs

# Build to verify no compilation errors
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj

git add src/FocusDeck.Server/
```

### Phase 5: Resolve Frontend
```bash
# Merge React App.tsx
code src/FocusDeck.WebApp/src/App.tsx

# Verify TypeScript compilation
cd src/FocusDeck.WebApp && npm run build

git add src/FocusDeck.WebApp/
```

### Phase 6: Fix Tests
```bash
# Resolve each test file
code tests/FocusDeck.Server.Tests/FocusSessionTests.cs
code tests/FocusDeck.Server.Tests/ForcedLogoutPropagationTests.cs
code tests/FocusDeck.Server.Tests/JarvisControllerTests.cs

# Run tests
dotnet test

git add tests/
```

### Phase 7: Final Validation
```bash
# Build entire solution
dotnet build FocusDeck.sln

# Run all tests
dotnet test

# BMAD checks
./bmad build
./bmad adapt

# Commit merge
git commit -m "Merge feature-browser-bridge-memory-vault into feature-gemini-embeddings-pivot (PR #10)

- Resolved AutomationTrigger schema conflicts
- Merged automation controller endpoints
- Integrated browser bridge and memory vault features
- Updated tests for new domain model

Closes #10"
```

### Phase 8: Push and Verify
```bash
# Push resolution branch
git push origin merge-resolution/browser-bridge-into-gemini

# Create new PR or update existing PR #10
# Verify CI/CD pipeline passes
```

## Key Decision Points

### AutomationTrigger Schema
**Decision Required:** Which branch's `AutomationTrigger` definition should be the source of truth?

**Recommendation:** Use `feature-browser-bridge-memory-vault` version since it:
- Has more recent commits (b9df3b6 vs 57c7f21)
- Includes comprehensive automation engine implementation
- Has `Type` and `Configuration` properties needed for flexibility

### Migration Strategy
If `AutomationTrigger` schema changed significantly:
1. Create EF Core migration: `dotnet ef migrations add MergeAutomationTriggerSchema -p src/FocusDeck.Persistence`
2. Review migration for data loss risks
3. Consider data migration script if production data exists

## Risk Assessment

**High Risk:**
- Database schema incompatibility could cause data loss
- Automation engine may have breaking changes in trigger evaluation

**Medium Risk:**
- Frontend API contract changes may break existing clients
- Test coverage may drop if tests are incorrectly merged

**Low Risk:**
- SignalR hub changes are additive (new methods)
- Controller endpoints are additive (new routes)

## Success Criteria

- [ ] All 9 conflicts manually resolved
- [ ] `dotnet build FocusDeck.sln` succeeds with 0 errors
- [ ] `dotnet test` passes with 70%+ coverage
- [ ] `./bmad build && ./bmad adapt` complete successfully
- [ ] No regression in existing automation features
- [ ] New browser bridge features functional
- [ ] New memory vault API accessible

## Contact

For questions or assistance with conflict resolution:
- Review PR #10 discussion: https://github.com/dertder25t-png/FocusDeck/pull/10
- Check commit history of both branches for context
- Test automation features after merge in development environment

---

**Last Updated:** November 20, 2025  
**Conflict Count:** 9 files  
**Estimated Resolution Time:** 3-4 hours (experienced developer)
