# JARVIS Canary Tenant - Operations & QA Guide

**Date**: November 14, 2025  
**Canary Tenant**: `FD86A760-06C6-4310-BEBB-4B2DC33295C6` (dertder25t@gmail.com)  
**Status**: ⚠️ Jarvis feature disabled pending PostgreSQL alignment

---

## 1. Feature Flag Status
### Configuration
- **Server Config**: `appsettings.Production.json` → `Features:Jarvis: false` (halt canary until the stack uses PostgreSQL per `OPERATIONS.md`)
- **Tenant ID**: `FD86A760-06C6-4310-BEBB-4B2DC33295C6`
- **Enabled At**: November 14, 2025

### Verification
```bash
# Check server config
grep -A 2 "Features" /home/focusdeck/FocusDeck/publish/appsettings.Production.json

# Expected output:
# "Features": {
#   "Jarvis": false
# }
```

---

## 2. Jarvis API Endpoints

### Available Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/v1/jarvis/workflows` | GET | List available workflows | ✅ Yes |
| `/v1/jarvis/run-workflow` | POST | Enqueue workflow execution | ✅ Yes |
| `/v1/jarvis/runs/{id}` | GET | Check workflow run status | ✅ Yes |
| `/v1/activity/signals` | POST | Submit activity signal | ✅ Yes |

### Testing Workflows Endpoint

```bash
# Test workflows list (requires auth token)
curl -X GET https://focusdeckv1.909436.xyz/v1/jarvis/workflows \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"

# Expected response (Phase 3.1 - empty or placeholder workflows):
# []
# or
# [{"id": "workflow-id", "name": "Workflow Name", "description": "..."}]
```

### Running a Workflow

```bash
# Enqueue a workflow run
curl -X POST https://focusdeckv1.909436.xyz/v1/jarvis/run-workflow \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "WorkflowId": "jarvis-phase-1-activity-detection",
    "Parameters": {}
  }'

# Expected success response:
# {
#   "RunId": "uuid-here",
#   "Status": "Pending",
#   "EnqueuedAt": "2025-11-14T06:45:00Z"
# }

# Expected 429 rate limit response:
# HTTP 429 Too Many Requests
# {
#   "error": "Run limit exceeded. Try again later."
# }
```

### Checking Run Status

```bash
# Get run status
curl -X GET https://focusdeckv1.909436.xyz/v1/jarvis/runs/{RUN_ID} \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# Expected response:
# {
#   "RunId": "uuid-here",
#   "Status": "Pending|Running|Completed|Failed",
#   "StartedAt": "...",
#   "CompletedAt": "...",
#   "Result": "..."
# }
```

---

## 3. Activity Signals System

### Purpose
Activity signals feed real-time user activity data into Jarvis workflows for context-aware automation.

### Signal Types

| SignalType | Description | Example Value |
|------------|-------------|---------------|
| `window_focus` | Window/app changed | `"Visual Studio Code"` |
| `keyboard_activity` | Typing detected | `"50"` (chars/min) |
| `mouse_activity` | Mouse movement | `"active"` |
| `app_launch` | App started | `"Chrome"` |
| `idle_start` | User went idle | `"300"` (seconds) |
| `idle_end` | User returned | `"active"` |
| `study_session_start` | Study timer started | `"Math 101"` |
| `study_session_end` | Study timer ended | `"45"` (minutes) |

### Submitting Activity Signals

```bash
# Submit a signal
curl -X POST https://focusdeckv1.909436.xyz/v1/activity/signals \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "SignalType": "window_focus",
    "SignalValue": "Visual Studio Code",
    "SourceApp": "FocusDeck.Desktop",
    "CapturedAtUtc": "2025-11-14T06:45:00Z",
    "MetadataJson": "{\"documentPath\": \"/src/app.ts\"}"
  }'

# Expected response:
# HTTP 202 Accepted
# {
#   "Id": "signal-uuid-here"
# }
```

### Activity Signal Emitter Script

Create a test script to generate synthetic signals:

```bash
#!/bin/bash
# File: /tmp/jarvis_signal_emitter.sh

TOKEN="YOUR_AUTH_TOKEN_HERE"
API_URL="https://focusdeckv1.909436.xyz/v1/activity/signals"

while true; do
  TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
  
  # Simulate window focus change
  curl -s -X POST "$API_URL" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"SignalType\": \"window_focus\",
      \"SignalValue\": \"$(shuf -n1 -e 'Chrome' 'VSCode' 'Notion' 'Slack')\",
      \"SourceApp\": \"TestEmitter\",
      \"CapturedAtUtc\": \"$TIMESTAMP\"
    }" > /dev/null
  
  echo "$(date): Sent window_focus signal"
  sleep 10
  
  # Simulate keyboard activity
  curl -s -X POST "$API_URL" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"SignalType\": \"keyboard_activity\",
      \"SignalValue\": \"$(shuf -i 10-100 -n1)\",
      \"SourceApp\": \"TestEmitter\",
      \"CapturedAtUtc\": \"$TIMESTAMP\"
    }" > /dev/null
  
  echo "$(date): Sent keyboard_activity signal"
  sleep 10
done
```

---

## 4. Monitoring & Observability

### Server Logs

#### Watch Jarvis-specific logs
```bash
# Watch all Jarvis activity
journalctl -u focusdeck -f | grep -i "jarvis"

# Watch for workflow runs
journalctl -u focusdeck -f | grep "run-workflow"

# Watch for activity signals
journalctl -u focusdeck -f | grep "Activity signal captured"

# Watch for rate limit 429s
journalctl -u focusdeck -f | grep "429\|rate limit\|Run limit exceeded"
```

#### Key Log Patterns to Monitor

| Pattern | Meaning | Action |
|---------|---------|--------|
| `Jarvis run requested. WorkflowId=...` | Workflow started | ✅ Normal |
| `Jarvis run limit exceeded` | Rate limiting active | ⚠️ Monitor frequency |
| `Activity signal captured: window_focus @ ...` | Signal received | ✅ Normal |
| `SQLite Error` or `Exception` | Database/app error | 🔴 Investigate |
| `SignalR.*connected` | Client connected to SignalR | ✅ Normal |
| `SignalR.*disconnected` | Client disconnected | ⚠️ Check client health |

### Database Monitoring

Production uses PostgreSQL for both the primary `focusdeck` database and the Hangfire `focusdeck_jobs` database (see `OPERATIONS.md#database-management`). Mirror the `ConnectionStrings__DefaultConnection` and `ConnectionStrings__HangfireConnection` values when you run these commands so the queries target the live Postgres hosts. Set `PGPASSWORD` (or populate `~/.pgpass`) before running the CLI snippets below.

```bash
PG_DEFAULT="${PG_DEFAULT:-"host=localhost port=5432 dbname=focusdeck user=postgres"}"
PG_HANGFIRE="${PG_HANGFIRE:-"host=localhost port=5432 dbname=focusdeck_jobs user=postgres"}"
```

#### Check workflow run history

```bash
PGPASSWORD="${PGPASSWORD:-yourpassword}" psql "$PG_DEFAULT" -c "SELECT Id, WorkflowId, Status, EnqueuedAtUtc, CompletedAtUtc FROM JarvisWorkflowRuns ORDER BY EnqueuedAtUtc DESC LIMIT 10;"
```

#### Check activity signal volume

```bash
PGPASSWORD="${PGPASSWORD:-yourpassword}" psql "$PG_DEFAULT" -c "SELECT SignalType, COUNT(*) AS Count, MIN(CapturedAtUtc) AS FirstSeen, MAX(CapturedAtUtc) AS LastSeen FROM ActivitySignals WHERE CapturedAtUtc > NOW() - INTERVAL '1 hour' GROUP BY SignalType ORDER BY Count DESC;"
```

#### Check activity signal latency

```bash
PGPASSWORD="${PGPASSWORD:-yourpassword}" psql "$PG_DEFAULT" -c "SELECT SignalType, SignalValue, CapturedAtUtc, SourceApp FROM ActivitySignals ORDER BY CapturedAtUtc DESC LIMIT 20;"
```

#### Inspect Hangfire queue depth

```bash
PGPASSWORD="${PGPASSWORD:-yourpassword}" psql "$PG_HANGFIRE" -c "SELECT StateName, COUNT(*) AS Count FROM hangfire.job GROUP BY StateName ORDER BY Count DESC;"
```

### Performance Metrics to Track

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Workflow API Latency** | <200ms | Time `curl` requests |
| **Signal Ingestion Rate** | <50ms | Check `Activity signal captured` log timestamps |
| **Database Write Time** | <10ms | EF Core logs with `DbCommand` timing |
| **HTTP 429 Rate** | <1% of requests | Count 429 responses vs total requests |
| **SignalR Connection Uptime** | >99% | Monitor disconnection frequency |
| **Memory Usage** | <500MB baseline | `ps aux | grep FocusDeck.Server` |
| **CPU Usage** | <10% idle, <50% active | `top -p $(pgrep -f FocusDeck.Server)` |

### SignalR Monitoring

```bash
# Watch SignalR connection events
journalctl -u focusdeck -f | grep "SignalR"

# Check active connections (if SignalR metrics exposed)
# This requires custom implementation in NotificationsHub
curl https://focusdeckv1.909436.xyz/healthz
```

---

## 5. Testing Workflow

### Step 1: Verify Jarvis is Enabled

```bash
# Check server logs for Jarvis initialization
journalctl -u focusdeck --since "5 minutes ago" | grep -i "jarvis\|workflow"

# Test workflows endpoint
curl -X GET https://focusdeckv1.909436.xyz/v1/jarvis/workflows \
  -H "Authorization: Bearer YOUR_TOKEN" | jq .
```

### Step 2: Generate Activity Signals

```bash
# Start the signal emitter
chmod +x /tmp/jarvis_signal_emitter.sh
/tmp/jarvis_signal_emitter.sh &

# Monitor signal ingestion
journalctl -u focusdeck -f | grep "Activity signal captured"
```

### Step 3: Trigger Workflow Run

```bash
# Run a workflow
WORKFLOW_ID="jarvis-phase-1-activity-detection"
RUN_RESPONSE=$(curl -s -X POST https://focusdeckv1.909436.xyz/v1/jarvis/run-workflow \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"WorkflowId\": \"$WORKFLOW_ID\", \"Parameters\": {}}")

echo "$RUN_RESPONSE" | jq .

# Extract RunId
RUN_ID=$(echo "$RUN_RESPONSE" | jq -r '.RunId')
echo "Run ID: $RUN_ID"
```

### Step 4: Monitor Workflow Execution

```bash
# Poll run status
while true; do
  curl -s -X GET "https://focusdeckv1.909436.xyz/v1/jarvis/runs/$RUN_ID" \
    -H "Authorization: Bearer YOUR_TOKEN" | jq .
  sleep 5
done

# Watch server logs
journalctl -u focusdeck -f | grep -E "Jarvis|WorkflowId=$WORKFLOW_ID"
```

### Step 5: Validate Results

```bash
# Check JarvisWorkflowRuns table
PG_DEFAULT="${PG_DEFAULT:-"host=localhost port=5432 dbname=focusdeck user=postgres"}"
PGPASSWORD="${PGPASSWORD:-yourpassword}"
export PGPASSWORD
psql "$PG_DEFAULT" -c "SELECT * FROM JarvisWorkflowRuns WHERE Id = '$RUN_ID';"

# Check for errors
journalctl -u focusdeck --since "10 minutes ago" | grep -i "error\|exception" | grep -i "jarvis"
```

---

## 6. Rate Limiting & HTTP 429

### Current Implementation
- **JarvisWorkflowRegistry**: Built-in rate limiting to prevent abuse
- **429 Response**: Returned when run limit exceeded

### Testing Rate Limits

```bash
# Spam workflow runs to trigger 429
for i in {1..20}; do
  curl -s -X POST https://focusdeckv1.909436.xyz/v1/jarvis/run-workflow \
    -H "Authorization: Bearer YOUR_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"WorkflowId": "test-workflow"}' | jq '.error'
  sleep 0.5
done

# Expected: First few succeed, then 429 errors
```

### Monitoring 429 Responses

```bash
# Count 429s in last hour
journalctl -u focusdeck --since "1 hour ago" | grep "429" | wc -l

# Extract 429 error messages
journalctl -u focusdeck --since "1 hour ago" | grep "Run limit exceeded"
```

---

## 7. UX/Ops Feedback Collection

### Feedback Template

Create a new entry for each observation period (daily/weekly):

```markdown
## Observation Period: [DATE RANGE]

### Performance Observations
- **Workflow API Latency**: [MEASUREMENT]
- **Signal Ingestion Rate**: [SIGNALS/SEC]
- **Database Write Latency**: [MS]
- **HTTP 429 Rate**: [PERCENTAGE]
- **Server CPU/Memory**: [BASELINE/PEAK]

### Reliability Issues
- [ ] Any 500 errors? (List trace IDs)
- [ ] Any SignalR disconnections?
- [ ] Any database lock timeouts?
- [ ] Any workflow run failures?

### UX Feedback
- **Most Useful Signals**: [LIST]
- **Most Problematic Signals**: [LIST]
- **Workflow Discovery**: Easy/Hard?
- **Run Status Visibility**: Clear/Unclear?

### Improvement Ideas
1. **Richer Metadata**: 
   - What additional context would improve workflow decisions?
   - Example: Add `{ "projectName": "...", "fileType": "..." }` to window_focus
   
2. **Activity Classification**:
   - Can we auto-detect "coding" vs "researching" vs "communicating"?
   - Should we add semantic tags like `activity_category: "productive" | "distraction"`?
   
3. **Workflow Enhancements**:
   - Which workflows would benefit from parallel execution?
   - Should we add workflow chaining/dependencies?

### Next Improvement Cycle Priorities
- [ ] Priority 1: [DESCRIPTION]
- [ ] Priority 2: [DESCRIPTION]
- [ ] Priority 3: [DESCRIPTION]
```

### Automated Metrics Collection

Instrument this script with PostgreSQL connections that match the production `ConnectionStrings__DefaultConnection` and `ConnectionStrings__HangfireConnection`. Populate `PGPASSWORD` or `~/.pgpass` before running.

```bash
#!/bin/bash
# File: /tmp/jarvis_metrics_collector.sh
# Run hourly via cron after the stack is aligned with PostgreSQL.

PG_DEFAULT="${PG_DEFAULT:-"host=localhost port=5432 dbname=focusdeck user=postgres"}"
PG_HANGFIRE="${PG_HANGFIRE:-"host=localhost port=5432 dbname=focusdeck_jobs user=postgres"}"
PGPASSWORD="${PGPASSWORD:-yourpassword}"
export PGPASSWORD

REPORT_FILE="/tmp/jarvis_metrics_$(date +%Y%m%d_%H%M).txt"

{
  echo "=== JARVIS Metrics Report ==="
  echo "Generated: $(date)"
  echo ""

  echo "--- Workflow Runs (Last Hour) ---"
  psql "$PG_DEFAULT" -c "SELECT Status, COUNT(*) FROM JarvisWorkflowRuns WHERE EnqueuedAtUtc > NOW() - INTERVAL '1 hour' GROUP BY Status;"

  echo ""
  echo "--- Activity Signals (Last Hour) ---"
  psql "$PG_DEFAULT" -c "SELECT SignalType, COUNT(*) FROM ActivitySignals WHERE CapturedAtUtc > NOW() - INTERVAL '1 hour' GROUP BY SignalType;"

  echo ""
  echo "--- Hangfire Queue State ---"
  psql "$PG_HANGFIRE" -c "SELECT StateName, COUNT(*) FROM hangfire.job GROUP BY StateName ORDER BY StateName;"

  echo ""
  echo "--- Server Resource Usage ---"
  ps aux | grep FocusDeck.Server | grep -v grep || true

  echo ""
  echo "--- HTTP 429 Count ---"
  journalctl -u focusdeck --since "1 hour ago" | grep "429" | wc -l

  echo ""
  echo "--- Recent Errors ---"
  journalctl -u focusdeck --since "1 hour ago" | grep -i "error\|exception" | tail -5

} > "$REPORT_FILE"

echo "Metrics report saved to: $REPORT_FILE"
```

---

## 8. Next Steps After Canary Testing

### Phase 3.2 Goals
Based on canary feedback, prioritize:

1. **If Performance Good**:
   - Expand to 10% of tenants
   - Add more workflow types
   - Implement workflow persistence

2. **If Metadata Insufficient**:
   - Extend `ActivitySignal.MetadataJson` schema
   - Add client-side metadata collectors
   - Document recommended metadata fields

3. **If Activity Classification Needed**:
   - Add `ActivitySignal.Category` field
   - Implement ML-based classification
   - Create activity taxonomy

4. **If Rate Limiting Issues**:
   - Adjust rate limit thresholds
   - Implement per-tenant quotas
   - Add backpressure mechanisms

### Graduation Criteria

- [ ] 100+ workflow runs completed
- [ ] 1000+ activity signals ingested
- [ ] <1% error rate
- [ ] <200ms P95 latency
- [ ] Zero critical bugs
- [ ] Positive UX feedback
- [ ] Clear improvement priorities identified

---

## Quick Reference Commands

```bash
# Jarvis remains disabled until the stack is on PostgreSQL.
grep -A 2 "Features" /home/focusdeck/FocusDeck/publish/appsettings.Production.json

# Restart server if you flip the feature flag.
sudo systemctl restart focusdeck

# PostgreSQL smoke checks (align with OPERATIONS.md env var names)
PG_DEFAULT="${PG_DEFAULT:-"host=localhost port=5432 dbname=focusdeck user=postgres"}"
PG_HANGFIRE="${PG_HANGFIRE:-"host=localhost port=5432 dbname=focusdeck_jobs user=postgres"}"
export PGPASSWORD="${PGPASSWORD:-yourpassword}"

psql "$PG_DEFAULT" -c "SELECT 1;"
psql "$PG_DEFAULT" -c "SELECT WorkflowId, Status, EnqueuedAtUtc FROM JarvisWorkflowRuns ORDER BY EnqueuedAtUtc DESC LIMIT 5;"
psql "$PG_HANGFIRE" -c "SELECT StateName, COUNT(*) FROM hangfire.job GROUP BY StateName ORDER BY StateName;"

# Watch Jarvis/Hangfire logs
journalctl -u focusdeck -f | grep -i jarvis
journalctl -u focusdeck -f | grep -i hangfire

# Test Jarvis endpoints once PostgreSQL is live
curl -X GET https://focusdeckv1.909436.xyz/v1/jarvis/workflows \
  -H "Authorization: Bearer TOKEN"

curl -X POST https://focusdeckv1.909436.xyz/v1/activity/signals \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"SignalType":"window_focus","SignalValue":"VSCode","SourceApp":"Test"}'
```

---

**Document Status**: ✅ Ready for Canary Testing  
**Last Updated**: November 14, 2025  
**Next Review**: After 24 hours of canary operation
