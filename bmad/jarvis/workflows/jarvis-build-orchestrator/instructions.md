# JARVIS Build Orchestrator - Master Conductor Instructions

**Workflow:** {project-root}/bmad/core/tasks/workflow.xml  
**Configuration:** {project-root}/bmad/jarvis/workflows/jarvis-build-orchestrator/workflow.yaml  
**Communicate in:** {communication_language}

---

## Welcome, {user_name}!

You are about to embark on **THE JARVIS JOURNEY** - a 6-month epic to transform FocusDeck from a productivity tracker into an autonomous AI companion for academic success.

This orchestrator coordinates all 6 phases. Choose your path, then invoke individual phase workflows.

---

## Available Paths

**Path 1: Sequential Build** (Recommended for teams)
- Build all phases in order: Phase 1  2  3  4  5  6
- Timeline: 26 weeks total
- Best for: Team continuity, staged MVP deployment

**Path 2: Parallel Build** (For larger teams)
- Team A: Phases 1-2 simultaneously
- Team B: Phases 3-4 simultaneously
- Converge for Phase 5-6
- Timeline: 13 weeks
- Best for: Multiple teams, faster delivery

**Path 3: Phase Selection** (Advanced)
- Pick specific phases: e.g., just Phase 1 + Phase 4
- Best for: Continuation builds or targeted sprints

**Path 4: MVP Only** (Fastest)
- Build Phase 1 only
- Deploy activity detection foundation
- Plan Phase 2+ later
- Timeline: 4 weeks
- Best for: Proof of concept

---

<workflow>

<step n= 1 goal=Confirm build readiness>
<action>Verify prerequisites are complete:

- Latest code pulled (git pull master)
- Clean build: dotnet build passes
- Tests passing: dotnet test passes
- Feature branch created: feature/jarvis-[phase]
- Team familiar with: JARVIS_QUICK_START.md

If any missing, pause and complete setup.</action>

<ask>Ready to proceed? (yes/no)</ask>
</step>

<step n=2 goal=Select your build path>
<ask>Which path matches your situation?

1. Sequential Build - Build all phases 1-6 in order
2. Parallel Build - Assign teams to phases 1-2 and 3-4
3. Specific Phases - Choose which phases to build
4. MVP Only - Build Phase 1 only for quick MVP

Enter: 1, 2, 3, or 4</ask>

<action>Store selected path for workflow coordination</action>
</step>

<step n=3 goal=Launch individual phase workflows>
<action>Based on selected path, invoke the appropriate phase workflow:

**Available phase workflows:**
- {phase_1_workflow} - Activity Detection (Weeks 1-4)
- {phase_2_workflow} - Burnout Detection (Weeks 5-8)
- {phase_3_workflow} - Notification Management (Weeks 9-12)
- {phase_4_workflow} - Content Generation (Weeks 13-20)
- {phase_5_workflow} - Workspace Automation (Weeks 21-24)
- {phase_6_workflow} - Metrics & Personalization (Weeks 25-26)

Each phase is a complete, standalone workflow with:
- Detailed week-by-week tasks
- Copy-paste ready code
- Integration instructions
- Testing strategy
- Success checklist
</action>
</step>

<step n=4 goal=Build and validate each phase>
<action>After each phase completes:
1. Full build: dotnet build
2. Run tests: dotnet test
3. Review: Check for errors or warnings
4. Commit: git commit with phase marker

Continue to next phase only if all green.</action>
</step>

<step n=5 goal=Track overall progress>
<template-output>orchestrator_progress</template-output>

<action>Generate master build report summarizing:
- Timeline: Start  Expected End
- Phases completed
- Build statistics
- Next steps
</action>
</step>

</workflow>

---

## Quick Start: Just Build Phase 1

If you want to jump straight into Phase 1 Activity Detection (4 weeks):

1. Invoke workflow: jarvis-phase-1-activity-detection
2. Follow Week 1-4 tasks
3. Read: {phase_1_detailed_guide} for detailed guidance
4. Reference: {integration_guide} while coding
5. Deploy Phase 1 MVP
6. Return here for Phase 2

---

## Phase Overview Matrix

| Phase | Duration | Difficulty | Status | Start Anytime |
|-------|----------|------------|--------|---------------|
| 1 | 4 weeks | Medium | Ready | Yes - Foundation |
| 2 | 4 weeks | Medium | Ready | Yes (after Phase 1) |
| 3 | 4 weeks | Easy | Ready | Yes (after Phase 1) |
| 4 | 8 weeks | Hard | Ready | Yes (after Phase 1) |
| 5 | 4 weeks | Medium | Ready | Yes (after Phase 1) |
| 6 | 2 weeks | Easy | Ready | Yes (after Phase 5) |

---

**Next Action:** Choose your path above, then invoke the first phase workflow!

May your focus be strong. 

