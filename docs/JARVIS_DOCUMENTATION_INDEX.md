#  JARVIS Documentation Index

**Last Updated:** November 5, 2025  
**Status:** Complete & Ready to Build (MCP + Docker Gateway Architecture)  
**Architecture:** Model Context Protocol (MCP) + Docker MCP Gateway (Recommended)

---

##  Quick Navigation

### Start Here (Pick Your Path)

####  **I Want to Build NOW** (Developer)
1. Read: JARVIS_QUICK_START.md (10 mins) — **Includes MCP overview**
2. Read: JARVIS_MCP_ARCHITECTURE.md (30 mins) — **NEW: Gateway & tool servers**
3. Read: JARVIS_PHASE1_DETAILED.md (60 mins) — **Updated: 6-week MCP-first plan**
4. Reference: JARVIS_INTEGRATION_WITH_FOCUSDECK.md (while coding)
5. Branch: `feature/jarvis-mcp-gateway`
6. Start: Task 1.1 (MCP Gateway scaffold)

####  **I Want to Understand the Vision** (Product/Stakeholder)
1. Read: JARVIS_QUICK_START.md (10 mins)
2. Read: JARVIS_IMPLEMENTATION_ROADMAP.md (20 mins)
3. Read: JARVIS_MCP_ARCHITECTURE.md Architecture Rationale section (10 mins)
4.  You now understand JARVIS + why MCP is the foundation

####  **I Want the Architecture** (Architect)
1. Read: JARVIS_MCP_ARCHITECTURE.md (entire document, 40 mins) — **NEW**
2. Read: JARVIS_INTEGRATION_WITH_FOCUSDECK.md (30 mins)
3. Review: MCP tool registry in JARVIS_PHASE1_DETAILED.md (10 mins)
4.  You now understand the full MCP-gateway-tool-stack

####  **I Want the Timeline** (Manager)
1. Read: JARVIS_QUICK_START.md Timeline section (5 mins)
2. Read: JARVIS_PHASE1_DETAILED.md Overview (5 mins) — **Now 6 weeks (was 4)**
3.  You have the updated 6-month plan with MCP integration

---

##  Document Directory

### In Project Root
- **JARVIS_ROADMAP_COMPLETE.md**  Overview & getting started

### In /docs folder

| Document | Purpose | Audience | Read Time | Status |
|----------|---------|----------|-----------|--------|
| **JARVIS_QUICK_START.md** | Start here! Vision + MCP intro | Everyone | 10 min |  Updated |
| **JARVIS_MCP_ARCHITECTURE.md** | Gateway, tool servers, schemas | Developers, Architects | 40 min |  NEW |
| **JARVIS_IMPLEMENTATION_ROADMAP.md** | Full 6-month plan (6 phases) | PMs, Architects | 20-30 min |  Updated |
| **JARVIS_PHASE1_DETAILED.md** | Build Phase 1 (6 weeks MCP-first) | Developers | 90-120 min |  Updated |
| **JARVIS_INTEGRATION_WITH_FOCUSDECK.md** | How JARVIS fits in FocusDeck | Developers, Architects | 30-40 min |  Updated |

---

##  Document Map (What Each Contains)

### JARVIS_QUICK_START.md (Updated)
`
 Documentation Map (this doc)
 What is JARVIS? (90 second explainer + MCP rationale)
 Why MCP + Docker Gateway? (1-minute decision summary)
 6-Month Plan (visual timeline, 6 weeks Phase 1 with MCP)
 Phase 1 Week-by-Week (gateway + 3 tools + write guards)
 Architecture (MCP-centric, how it fits)
 FAQ (common questions + MCP-specific Q&A)
 Next Steps (today's action)
`

### JARVIS_MCP_ARCHITECTURE.md (NEW)
`
 What is MCP? (background for architects/leads)
 Docker MCP Gateway (infrastructure overview)
 Tool Server Pattern (how to implement each integration)
 OIDC + JWT Flow (security model)
 Tool Registry & Discovery (how Jarvis finds tools)
 Schema Definition (request/response formats)
 Rate Limiting & Audit Logging (gateway responsibilities)
 Example: Canvas MCP Server (step-by-step implementation)
 Example: Spotify MCP Server (another tool)
 Local Development (docker-compose setup)
 Production Deployment (Cloudflare + Linux server)
 Comparison: Before (Direct APIs) vs. After (MCP)
`

### JARVIS_IMPLEMENTATION_ROADMAP.md (Updated)
`
 Executive Vision
 Why MCP? (architecture rationale, integration velocity)
 All 6 Phases (Phases 1–6, Phase 1 now 6 weeks)
 Phase 1: MCP Foundation (gateway + first 3 tools)
 Phase 2–6: (build on gateway)
 Tool Registry (all integrations to register)
 Architecture & Services
 Deliverables by Phase
 Testing Strategy (MCP-specific)
 Success Metrics
 Timeline Summary (6 weeks Phase 1 instead of 4)
 Team Requirements
 Technical Risks & Mitigation
 Priority Ranking
`

### JARVIS_PHASE1_DETAILED.md (Updated)
`
 Phase 1 Overview (6 weeks, MCP-first)
 Week 1: MCP Gateway Scaffold
  - Task 1.1: Docker MCP Gateway deployment (local + production config)
  - Task 1.2: OIDC/JWT integration, CORS setup
  - Task 1.3: Tool registry & discovery endpoint
  -  Copy-paste docker-compose + config YAML

 Week 2: First 3 Tools (Read-Only)
  - Task 2.1: Canvas MCP Server (canvas.listDue, canvas.listCourses)
  - Task 2.2: Google Calendar MCP Server (calendar.listUpcoming)
  - Task 2.3: Notes MCP Server (notes.search)
  -  Copy-paste tool server code (C# + MCP SDK)

 Week 3: Write Tools + Guardrails
  - Task 3.1: Calendar Write (calendar.blockStudy)
  - Task 3.2: Canvas Write (canvas.viewAssignment with rate limits)
  - Task 3.3: Confirm-Card UI (mobile + desktop ask-before-execute)
  -  Copy-paste guardrail middleware

 Week 4: Integration & Testing
  - Task 4.1: Jarvis agent calls MCP gateway (not direct APIs)
  - Task 4.2: SignalR broadcasts MCP tool results
  - Task 4.3: Database migrations for audit logs
  -  Copy-paste agent code + tests

 Week 5: Remaining Tools
  - Task 5.1: Spotify MCP Server
  - Task 5.2: Home Assistant MCP Server
  - Task 5.3: Gmail MCP Server
  -  Copy-paste code per tool

 Week 6: Observability
  - Task 6.1: Gateway audit logging (PII redaction)
  - Task 6.2: Rate limit enforcement & quotas
  - Task 6.3: Metrics dashboard (Grafana + Prometheus)
  -  Copy-paste log aggregation config

 Testing Strategy (per task + MCP-specific)
 Quality Bars (6-week success criteria)
 Task Checklist (all 18 tasks across 6 weeks)
`

### JARVIS_INTEGRATION_WITH_FOCUSDECK.md (Updated)
`
 Current FocusDeck Architecture (diagram)
 JARVIS + MCP Integration Points (now gateway-centric)
 MCP Gateway Layer (new layer in arch)
 Tool Servers Layer (Canvas, Calendar, etc. as MCP servers)
 Database Schema (audit logs, tool invocations)
 Service Registration (DI setup for MCP client)
 SignalR Hub Extensions (tool result broadcasts)
 Folder Structure (new /src/FocusDeck.Mcp/** folders)
 Docker Compose (updated with mcp-gateway, mcp-canvas, etc.)
 Deployment Strategy (gateway behind Cloudflare Tunnel)
 Backward Compatibility (non-MCP fallback for Phase 0)
 Migration Path (strangler pattern: 3 tools at a time)
`

---

##  Architecture Decision Matrix

### MCP + Docker Gateway vs. Direct APIs

| Aspect | Direct API Calls | MCP + Gateway (Recommended) |
|--------|-----------------|--------------------------|
| **Auth Plumbing** | Repeated per client (Desktop, Mobile, Server) | Centralized at gateway |
| **New Integration Time** | 1–2 days (touch Jarvis + each client + OAuth flows) | 2–4 hours (define schema, drop MCP server in docker-compose) |
| **Model Swaps** | Rebuild Jarvis agent logic | Change 1 line in gateway config |
| **Rate Limiting** | N/A or per-client custom code | Built-in per-user/per-service quotas |
| **Audit Trail** | None (or add custom logging) | Automatic (tool name, user, duration, quota hits) |
| **PII Redaction** | Manual per API call | One redaction policy at gateway |
| **Total Ops Cost (6 months)** | Lower Week 1, higher Month 3+ | Higher Week 1, lower Month 2+ |
| **Testability** | Mock each API client | Mock MCP tool responses (simpler) |
| **Long-term Velocity** | Slows down (N×M integrations) | Speeds up (register once, Jarvis auto-discovers) |

**Verdict:** MCP + Gateway wins for multi-client SaaS with heavy integration surface (your case).

---

##  Usage Scenarios

### Scenario 1: "I'm a developer ready to code MCP"
`
1. Git clone / pull latest
2. cat docs/JARVIS_MCP_ARCHITECTURE.md (understand gateway)
3. cat docs/JARVIS_PHASE1_DETAILED.md (see Week 1 tasks)
4. git checkout -b feature/jarvis-mcp-gateway
5. Start Task 1.1 (deploy gateway via docker-compose)
6. Reference JARVIS_MCP_ARCHITECTURE.md for tool server pattern
7. Commit, push, PR
`

### Scenario 2: "I need to add a new integration (Spotify)"
`
1. Open JARVIS_MCP_ARCHITECTURE.md Example: Spotify MCP Server
2. Copy the tool server template
3. Update service calls to call Spotify API (using token broker)
4. Define tool schema (input/output JSON)
5. Add to docker-compose services
6. Register tool in mcp-config.yaml
7. Jarvis auto-discovers tool on next startup
8. Done! (no Jarvis agent code changes needed)
`

### Scenario 3: "I need to explain MCP to stakeholders"
`
1. Open JARVIS_QUICK_START.md
2. Read "Why MCP?" section (1 minute)
3. Show comparison table above
4. Show before/after integration time (1 day → 4 hours)
5. You now have a 5-minute pitch
`

### Scenario 4: "I'm planning the next sprint"
`
1. Read JARVIS_QUICK_START.md (10 mins)
2. Read JARVIS_PHASE1_DETAILED.md Week 1–2 (1 hour)
3. You now know: Gateway setup (Week 1) + first 3 tools (Week 2)
4. Estimate: 6 weeks for Phase 1 (was 4 weeks, but with better foundation)
5. Plan Sprint 1: Week 1 (gateway) + Week 2 (tools Week 1 half)
`

### Scenario 5: "I want to understand the full MCP architecture"
`
1. Read JARVIS_MCP_ARCHITECTURE.md (40 mins)
2. Look at Docker Compose section
3. Look at Tool Server Pattern section
4. Look at OIDC + JWT Flow section
5. You now understand: gateway, auth, tool servers, schemas, discovery
`

---

##  Document Reading Order (Recommended)

### First Time? Follow This Path:

**1. JARVIS_QUICK_START.md (10 mins)**
   - Understand what JARVIS is
   - See "Why MCP?" section
   - See the updated 6-week timeline
   - Get excited!

**2. JARVIS_MCP_ARCHITECTURE.md (40 mins)**
   - Understand what MCP is
   - See gateway + tool servers diagram
   - Learn the OIDC + JWT flow
   - See Docker Compose setup

**3. JARVIS_IMPLEMENTATION_ROADMAP.md (20 mins)**
   - Read Phases 1–3 only (skip Phases 4–6 for now)
   - Understand tool registry
   - See deliverables with MCP

**4. JARVIS_PHASE1_DETAILED.md Week 1–2 (60 mins)**
   - Understand Week 1 (gateway scaffold)
   - Understand Week 2 (first 3 tools)
   - See code examples (docker-compose, tool server, schema)

**5. JARVIS_INTEGRATION_WITH_FOCUSDECK.md (as needed)**
   - Reference while coding
   - Check folder structure (new /src/FocusDeck.Mcp/** paths)
   - Check docker-compose setup
   - Check DI registration for MCP client

---

##  Checklist: Are You Ready to Build?

- [ ] Read JARVIS_QUICK_START.md (including "Why MCP?" section)
- [ ] Read JARVIS_MCP_ARCHITECTURE.md (gateway, auth, tools)
- [ ] Read JARVIS_PHASE1_DETAILED.md Week 1 (gateway scaffold)
- [ ] Understand MCP tool schema pattern (input/output JSON)
- [ ] Understand Docker Compose + MCP gateway config
- [ ] Have JARVIS_INTEGRATION_WITH_FOCUSDECK.md open
- [ ] Created feature branch: `feature/jarvis-mcp-gateway`
- [ ] Docker Desktop running locally
- [ ] Ready to write Task 1.1 code (gateway docker-compose)
- [ ]  **YOU'RE READY!**

---

##  Finding Specific Information

### "I need to find..."

**...why MCP instead of direct APIs**
 JARVIS_QUICK_START.md "Why MCP?" or this index (Architecture Decision Matrix)

**...the 6-week timeline**
 JARVIS_QUICK_START.md or JARVIS_PHASE1_DETAILED.md Overview

**...how to set up the Docker MCP Gateway**
 JARVIS_MCP_ARCHITECTURE.md "Docker MCP Gateway" section or JARVIS_PHASE1_DETAILED.md Task 1.1

**...how to implement a tool server (Canvas, Spotify, etc.)**
 JARVIS_MCP_ARCHITECTURE.md "Tool Server Pattern" or "Example: Canvas MCP Server"

**...Phase 1 tasks (Week 1–6)**
 JARVIS_PHASE1_DETAILED.md entire document

**...database schema for JARVIS + MCP audit logs**
 JARVIS_INTEGRATION_WITH_FOCUSDECK.md Database Schema section

**...code I can copy/paste**
 JARVIS_PHASE1_DETAILED.md (every week has code) or JARVIS_MCP_ARCHITECTURE.md (examples)

**...service interfaces to implement**
 JARVIS_MCP_ARCHITECTURE.md or JARVIS_PHASE1_DETAILED.md

**...success metrics for Phase 1**
 JARVIS_PHASE1_DETAILED.md Quality Bars section

**...how JARVIS + MCP connects to existing FocusDeck**
 JARVIS_INTEGRATION_WITH_FOCUSDECK.md entire document

**...OIDC + JWT flow for MCP gateway**
 JARVIS_MCP_ARCHITECTURE.md "OIDC + JWT Flow" section

**...how to add a new tool to the gateway**
 Scenario 2 above, or JARVIS_MCP_ARCHITECTURE.md "Tool Registry & Discovery"

**...docker-compose for local dev**
 JARVIS_MCP_ARCHITECTURE.md "Local Development" or JARVIS_INTEGRATION_WITH_FOCUSDECK.md

---

##  Questions? Here's Where to Find Answers

| Question | Document | Section |
|----------|----------|---------|
| What is JARVIS? | QUICK_START | "What is JARVIS?" |
| Why MCP + Gateway? | QUICK_START or this index | "Why MCP?" / Decision Matrix |
| How is MCP different from direct APIs? | MCP_ARCHITECTURE | "What is MCP?" |
| How long will Phase 1 take? | QUICK_START | Timeline |
| What do I build in Phase 1? | PHASE1_DETAILED | Week 1–6 Breakdown |
| How do I deploy the gateway locally? | MCP_ARCHITECTURE | Local Development |
| How do I implement a tool server? | MCP_ARCHITECTURE | Tool Server Pattern + Examples |
| Where does code go? | INTEGRATION | Folder Structure |
| What's the docker-compose setup? | MCP_ARCHITECTURE or INTEGRATION | Local Development / Docker Compose |
| How does Jarvis find tools? | MCP_ARCHITECTURE | Tool Registry & Discovery |
| What's the OIDC flow? | MCP_ARCHITECTURE | OIDC + JWT Flow |
| How do I add a new integration? | Scenario 2 above | (Spotify example) |
| What are the success criteria? | PHASE1_DETAILED | Quality Bars |
| Do I need new dependencies? | INTEGRATION | Deployment section |
| Can I ship Phase 1 alone? | ROADMAP | Phase 1 Overview |
| When do I need an AI team? | QUICK_START | FAQ section |

---

##  Success Milestones (6-Week Phase 1)

### By End of Week 1:
-  Docker MCP Gateway deployed locally
-  OIDC + JWT integration tested
-  Tool registry endpoint working
-  PR submitted for review

### By End of Week 2:
-  Canvas MCP Server implemented
-  Google Calendar MCP Server implemented
-  Notes MCP Server implemented
-  Integration tests passing

### By End of Week 3:
-  Write-tool guardrails in place (confirm-cards UI)
-  Calendar.blockStudy tool working
-  Canvas rate limits enforced
-  Mobile + Desktop UIs ask before executing

### By End of Week 4:
-  Jarvis agent calls MCP gateway (not direct APIs)
-  SignalR broadcasts tool results
-  Audit logging working
-  Integration tests passing

### By End of Week 5:
-  Spotify MCP Server implemented
-  Home Assistant MCP Server implemented
-  Gmail MCP Server implemented
-  Tool auto-discovery working

### By End of Week 6:
-  Gateway audit logging complete (PII redacted)
-  Rate limits + quota enforcement live
-  Metrics dashboard (Grafana) running
-  Phase 1 COMPLETE! 

---

##  Launch Sequence (6-Week MCP-First Plan)

`
TODAY:
  
  Read JARVIS_QUICK_START.md (10 mins)
  Read JARVIS_MCP_ARCHITECTURE.md (40 mins)
  
TOMORROW:
  
  Read JARVIS_PHASE1_DETAILED.md (90 mins)
  
THIS WEEK:
  
  Start Task 1.1 (deploy gateway via docker-compose)
  
WEEK 2:
  
  Complete Task 1.3 + start Week 2 (first 3 tools)
  
WEEK 3:
  
  Complete Week 2 tools + start Week 3 (write guardrails)
  
WEEK 4:
  
  Complete Week 3 + start Week 4 (Jarvis → MCP integration)
  
WEEK 5:
  
  Complete Week 4 + start Week 5 (Spotify, HA, Gmail tools)
  
WEEK 6:
  
  Complete Week 5 + observability (logs, metrics, audit trail)
  
PHASE 1 COMPLETE:
  
  Foundation ready for Phases 2–6
  
MID-JANUARY:
  
  All 6 Phases SHIPPED 
`

---

##  Document Versions

| Document | Version | Status | Last Updated |
|----------|---------|--------|--------------|
| JARVIS_QUICK_START.md | 2.0 |  Updated w/ MCP | Nov 5, 2025 |
| JARVIS_MCP_ARCHITECTURE.md | 1.0 |  NEW | Nov 5, 2025 |
| JARVIS_IMPLEMENTATION_ROADMAP.md | 2.0 |  Updated w/ MCP timeline | Nov 5, 2025 |
| JARVIS_PHASE1_DETAILED.md | 2.0 |  Updated 6-week MCP-first | Nov 5, 2025 |
| JARVIS_INTEGRATION_WITH_FOCUSDECK.md | 2.0 |  Updated w/ MCP folders | Nov 5, 2025 |
| JARVIS_ROADMAP_COMPLETE.md | 1.0 |  Final | Nov 5, 2025 |
| This Index | 2.0 |  Updated w/ MCP sections | Nov 5, 2025 |

---

##  You're All Set!

You now have everything you need to build JARVIS on MCP:
-  Vision & strategy (MCP-centric)
-  MCP architecture deep-dive
-  Implementation roadmap (6 weeks Phase 1)
-  Week-by-week tasks with MCP gateway
-  Copy-paste code (gateway, tool servers, schemas)
-  Integration guide (MCP + FocusDeck)
-  Success metrics

**Next Action:** Open JARVIS_QUICK_START.md and start reading the "Why MCP?" section!

**Questions?** Check the relevant document using the guide above.

**Ready to build?** Branch off `feature/jarvis-mcp-gateway` and start coding Task 1.1!

---

**May the focus be with you. (Now with MCP superpowers.)**
