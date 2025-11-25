# JARVIS Canary Deployment - Infrastructure Blockers & Operations Checklist

**Status**: 🔴 BLOCKED - Infrastructure Provisioning Required  
**Date**: November 14, 2025  
**Priority**: HIGH - Blocking Phase 1.5 and Phase 4 R&D

---

## Current Status

### What's Ready
- ✅ Jarvis feature code deployed to production server
- ✅ Database migrations created (`AddJarvisAndActivitySignals`)
- ✅ API endpoints implemented and tested
- ✅ Monitoring documentation created
- ✅ Testing scripts available

### What's Blocking
- ❌ PostgreSQL databases not provisioned
- ❌ Environment variables not configured
- ❌ Running on SQLite (dev database) instead of PostgreSQL (production)
- ❌ Hangfire background jobs not configured for production
- ❌ E2E validation not completed
- ❌ 24-hour monitoring campaign not started

---

## Infrastructure Requirements

### Database Provisioning

#### Required Databases

1. **`focusdeck`** - Main application database
   - Purpose: User data, auth, activity signals, workflow runs
   - Tables: `ActivitySignals`, `JarvisWorkflowRuns`, `PakeCredentials`, `Tenants`, etc.
   - Estimated Size: 1-5 GB initial
   - Connections: ~20-50 concurrent

2. **`focusdeck_jobs`** - Hangfire background jobs database
   - Purpose: Job queue, execution history, scheduling
   - Tables: Hangfire schema (managed automatically)
   - Estimated Size: 500 MB - 2 GB
   - Connections: ~10-20 concurrent

#### PostgreSQL Server Requirements

- **Version**: PostgreSQL 14+ (recommended 16)
- **RAM**: Minimum 2 GB dedicated to PostgreSQL
- **Storage**: 10 GB minimum, 50 GB recommended
- **Extensions**: None required (standard installation)
- **Backups**: Daily automated backups recommended

---

## Operations Tasks Checklist

### Task 1: Provision PostgreSQL Databases ⏳ PENDING

**Owner**: Operations/DevOps  
**Priority**: CRITICAL  
**Estimated Time**: 30-60 minutes

#### Steps

1. **Install PostgreSQL** (if not already installed)
   ```bash
   # Ubuntu/Debian
   sudo apt update
   sudo apt install postgresql postgresql-contrib
   
   # Start service
   sudo systemctl start postgresql
   sudo systemctl enable postgresql
   ```

2. **Create databases**
   ```bash
   # Switch to postgres user
   sudo -u postgres psql
   
   # Create databases
   CREATE DATABASE focusdeck;
   CREATE DATABASE focusdeck_jobs;
   
   # Create dedicated user (recommended)
   CREATE USER focusdeck_app WITH PASSWORD 'SECURE_PASSWORD_HERE';
   GRANT ALL PRIVILEGES ON DATABASE focusdeck TO focusdeck_app;
   GRANT ALL PRIVILEGES ON DATABASE focusdeck_jobs TO focusdeck_app;
   
   # Exit psql
   \q
   ```

3. **Configure PostgreSQL access**
   ```bash
   # Edit pg_hba.conf to allow password authentication
   sudo nano /etc/postgresql/*/main/pg_hba.conf
   
   # Add line (adjust for your setup):
   # host    focusdeck,focusdeck_jobs    focusdeck_app    127.0.0.1/32    md5
   
   # Reload PostgreSQL
   sudo systemctl reload postgresql
   ```

4. **Verify connectivity**
   ```bash
   # Test connection
   psql -h localhost -U focusdeck_app -d focusdeck -c "SELECT version();"
   ```

**Success Criteria**:
- [ ] Both databases created
- [ ] User `focusdeck_app` has full access
- [ ] Connection from localhost works
- [ ] Password authentication configured

---

### Task 2: Configure Environment Variables ⏳ PENDING

**Owner**: Operations/DevOps  
**Priority**: CRITICAL  
**Estimated Time**: 15-30 minutes

#### Steps

1. **Create systemd environment file**
   ```bash
   # Create environment file
   sudo nano /etc/systemd/system/focusdeck.service.d/override.conf
   ```

2. **Add environment variables**
   ```ini
   [Service]
   Environment="ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=focusdeck;Username=focusdeck_app;Password=SECURE_PASSWORD_HERE;Include Error Detail=true"
   Environment="ConnectionStrings__HangfireConnection=Host=localhost;Port=5432;Database=focusdeck_jobs;Username=focusdeck_app;Password=SECURE_PASSWORD_HERE"
   Environment="Features__Jarvis=true"
   Environment="ASPNETCORE_ENVIRONMENT=Production"
   
   # Optional: Increase connection pool size for production
   Environment="ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=focusdeck;Username=focusdeck_app;Password=SECURE_PASSWORD_HERE;Maximum Pool Size=50"
   ```

3. **Reload systemd and restart service**
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl restart focusdeck
   ```

4. **Verify environment variables are set**
   ```bash
   # Check service environment
   sudo systemctl show focusdeck --property=Environment
   
   # Verify connection in logs
   sudo journalctl -u focusdeck --since "1 minute ago" | grep -i "postgres\|connection"
   ```

**Success Criteria**:
- [ ] Environment variables set in systemd service
- [ ] Service restarts successfully
- [ ] PostgreSQL connection strings detected in logs
- [ ] No SQLite database files being created

---

### Task 3: Run Database Migrations ⏳ PENDING

**Owner**: Developer/Operations  
**Priority**: CRITICAL  
**Estimated Time**: 10-15 minutes

#### Steps

1. **Backup current SQLite data (if needed)**
   ```bash
   # Export critical data from SQLite
   sqlite3 /home/focusdeck/FocusDeck/data/focusdeck.db << 'EOF'
   .mode insert
   .output /tmp/focusdeck_sqlite_backup.sql
   
   SELECT * FROM PakeCredentials;
   SELECT * FROM Tenants;
   SELECT * FROM TenantUsers;
   
   .quit
   EOF
   ```

2. **Apply migrations to PostgreSQL**
   ```bash
   cd /root/FocusDeck
   
   # Set connection string for migration
   export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=focusdeck;Username=focusdeck_app;Password=SECURE_PASSWORD_HERE"
   
   # Run migrations
   dotnet ef database update --project src/FocusDeck.Persistence --startup-project src/FocusDeck.Server
   ```

3. **Verify schema**
   ```bash
   # Connect to PostgreSQL
   psql -h localhost -U focusdeck_app -d focusdeck
   
   # List tables
   \dt
   
   # Verify Jarvis tables exist
   SELECT table_name FROM information_schema.tables 
   WHERE table_schema = 'public' 
   AND table_name IN ('ActivitySignals', 'JarvisWorkflowRuns');
   
   # Exit
   \q
   ```

4. **Migrate critical data (if needed)**
   ```bash
   # Import user credentials from SQLite backup
   # This step depends on your data migration strategy
   ```

**Success Criteria**:
- [ ] All EF migrations applied to PostgreSQL
- [ ] `ActivitySignals` table exists
- [ ] `JarvisWorkflowRuns` table exists
- [ ] `PakeCredentials` table exists
- [ ] All indexes created
- [ ] Critical user data migrated (if applicable)

---

### Task 4: Configure Hangfire for PostgreSQL ⏳ PENDING

**Owner**: Developer/Operations  
**Priority**: HIGH  
**Estimated Time**: 15-20 minutes

#### Steps

1. **Verify Hangfire NuGet package**
   ```bash
   cd /root/FocusDeck/src/FocusDeck.Server
   
   # Check if Hangfire.PostgreSql is installed
   dotnet list package | grep Hangfire
   
   # If not, add it
   dotnet add package Hangfire.PostgreSql
   ```

2. **Update Program.cs configuration** (if needed)
   ```csharp
   // In Program.cs, ensure Hangfire uses PostgreSQL connection
   builder.Services.AddHangfire(config =>
   {
       config.UsePostgreSqlStorage(
           builder.Configuration.GetConnectionString("HangfireConnection"),
           new PostgreSqlStorageOptions
           {
               SchemaName = "hangfire"
           });
   });
   ```

3. **Restart server and verify Hangfire tables**
   ```bash
   sudo systemctl restart focusdeck
   
   # Check that Hangfire created its schema
   psql -h localhost -U focusdeck_app -d focusdeck_jobs
   
   # List Hangfire tables
   \dt hangfire.*
   
   # Should see: hangfire.job, hangfire.jobqueue, hangfire.server, etc.
   \q
   ```

**Success Criteria**:
- [ ] Hangfire.PostgreSql package installed
- [ ] Hangfire schema created in `focusdeck_jobs` database
- [ ] Background jobs processing (check `/hangfire` dashboard)
- [ ] No errors in logs related to Hangfire

---

### Task 5: Deploy with Features:Jarvis Enabled ⏳ PENDING

**Owner**: Operations/DevOps  
**Priority**: HIGH  
**Estimated Time**: 10-15 minutes

#### Steps

1. **Verify appsettings.Production.json**
   ```bash
   # Check current config
   cat /home/focusdeck/FocusDeck/publish/appsettings.Production.json
   
   # Should contain:
   # "Features": {
   #   "Jarvis": true
   # }
   ```

2. **Rebuild and deploy** (if needed)
   ```bash
   cd /root/FocusDeck
   
   # Build
   dotnet publish src/FocusDeck.Server -c Release -o /tmp/focusdeck-publish-postgres
   
   # Stop service
   sudo systemctl stop focusdeck
   
   # Backup old deployment
   sudo mv /home/focusdeck/FocusDeck/publish /home/focusdeck/FocusDeck/publish.backup.$(date +%Y%m%d_%H%M)
   
   # Deploy new build
   sudo mv /tmp/focusdeck-publish-postgres /home/focusdeck/FocusDeck/publish
   sudo chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck/publish
   
   # Start service
   sudo systemctl start focusdeck
   ```

3. **Verify Jarvis is enabled**
   ```bash
   # Check logs for Jarvis initialization
   sudo journalctl -u focusdeck --since "1 minute ago" | grep -i jarvis
   
   # Test Jarvis endpoint (should return 401, not 404)
   curl -v https://focusdeckv1.909436.xyz/v1/jarvis/workflows 2>&1 | grep "< HTTP"
   
   # Should see: < HTTP/2 401 (not 404)
   ```

**Success Criteria**:
- [ ] Server running with PostgreSQL connection
- [ ] `Features:Jarvis: true` in config
- [ ] `/v1/jarvis/workflows` returns 401 (auth required)
- [ ] No 404 or 500 errors on Jarvis endpoints
- [ ] Logs show "AddJarvisAndActivitySignals" migration applied

---

### Task 6: Execute E2E Validation ⏳ PENDING

**Owner**: QA/Operations  
**Priority**: HIGH  
**Estimated Time**: 30-45 minutes

#### Test Suite

Run all tests from `JARVIS_CANARY_OPS_GUIDE.md`:

##### 6.1 API Endpoint Validation
```bash
# Get auth token first (login via UI)
TOKEN="YOUR_AUTH_TOKEN_HERE"

# Test workflows endpoint
curl -X GET https://focusdeckv1.909436.xyz/v1/jarvis/workflows \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 OK with workflow list (may be empty in Phase 3.1)
```

##### 6.2 Activity Signal Ingestion
```bash
# Submit test signal
curl -X POST https://focusdeckv1.909436.xyz/v1/activity/signals \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "SignalType": "window_focus",
    "SignalValue": "Visual Studio Code",
    "SourceApp": "E2ETest",
    "CapturedAtUtc": "'$(date -u +"%Y-%m-%dT%H:%M:%SZ")'"
  }'

# Expected: 202 Accepted with signal ID
```

##### 6.3 Database Verification
```bash
# Verify signal was stored in PostgreSQL
psql -h localhost -U focusdeck_app -d focusdeck -c \
  "SELECT * FROM \"ActivitySignals\" ORDER BY \"CapturedAtUtc\" DESC LIMIT 5;"

# Should see the test signal
```

##### 6.4 Workflow Run Test
```bash
# Enqueue a workflow
curl -X POST https://focusdeckv1.909436.xyz/v1/jarvis/run-workflow \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "WorkflowId": "test-workflow",
    "Parameters": {}
  }'

# Expected: 200 OK with RunId (or appropriate error)
```

##### 6.5 Job Runner Verification
```bash
# Check Hangfire dashboard
curl -H "Authorization: Bearer $TOKEN" \
  https://focusdeckv1.909436.xyz/hangfire/

# Or check database directly
psql -h localhost -U focusdeck_app -d focusdeck_jobs -c \
  "SELECT * FROM hangfire.job ORDER BY createdat DESC LIMIT 10;"
```

##### 6.6 SignalR Connection Test
```bash
# Check SignalR hub is accessible
curl -v https://focusdeckv1.909436.xyz/notifications/negotiate 2>&1 | grep "< HTTP"

# Expected: Appropriate SignalR negotiation response
```

**Success Criteria**:
- [ ] All API endpoints return expected status codes
- [ ] Activity signals stored in PostgreSQL
- [ ] Workflow runs created in database
- [ ] Hangfire jobs processing
- [ ] SignalR hub accessible
- [ ] No 500 errors in logs

---

### Task 7: Commence 24-Hour Monitoring Campaign ⏳ PENDING

**Owner**: Operations/DevOps  
**Priority**: HIGH  
**Estimated Time**: 24 hours + analysis

#### Monitoring Setup

1. **Start continuous signal generation**
   ```bash
   # Run signal emitter in background
   nohup /tmp/jarvis_signal_emitter.sh "$TOKEN" 30 > /tmp/signal_emitter.log 2>&1 &
   
   # Record PID
   echo $! > /tmp/signal_emitter.pid
   ```

2. **Set up automated metrics collection**
   ```bash
   # Create hourly cron job
   crontab -e
   
   # Add line:
   # 0 * * * * /tmp/jarvis_metrics_collector.sh >> /var/log/jarvis_metrics.log 2>&1
   ```

3. **Monitor key metrics**
   ```bash
   # Watch logs continuously
   sudo journalctl -u focusdeck -f | grep -i "jarvis\|activity signal\|workflow"
   
   # Every hour, collect metrics:
   # - Signal ingestion rate
   # - Workflow execution count
   # - Database size growth
   # - Memory/CPU usage
   # - Error rate
   # - HTTP 429 count
   ```

4. **Document observations**
   - Use feedback template in `JARVIS_CANARY_OPS_GUIDE.md`
   - Record performance metrics
   - Note any errors or anomalies
   - Capture UX feedback

#### Metrics to Track

| Metric | Target | Collection Method |
|--------|--------|-------------------|
| Signal Ingestion Latency | <50ms | Timestamp diff in logs |
| API Response Time | <200ms | `curl -w "%{time_total}"` |
| Database Write Latency | <10ms | EF Core logs |
| HTTP 429 Rate | <1% | Count 429s vs total requests |
| Memory Usage | <500MB | `ps aux \| grep FocusDeck` |
| CPU Usage | <10% idle | `top -p $(pgrep FocusDeck)` |
| Error Rate | <0.1% | Count ERR logs vs total |
| Workflow Success Rate | >95% | Query `JarvisWorkflowRuns` status |

**Success Criteria**:
- [ ] 24 hours of continuous operation
- [ ] 1000+ activity signals ingested
- [ ] 100+ workflow runs attempted
- [ ] All performance targets met
- [ ] Error rate below threshold
- [ ] No critical bugs discovered
- [ ] Feedback documented

---

## Post-Monitoring Next Steps

Once 24-hour monitoring is complete and successful:

### Phase 1.5: Contextual Learning Loop
- [ ] R&D can proceed
- [ ] Design activity pattern recognition
- [ ] Implement context aggregation improvements
- [ ] Build learning feedback mechanisms

### Phase 4: Auto-Tagging & Burnout Detection
- [ ] R&D can proceed
- [ ] Design auto-tagging taxonomy
- [ ] Implement burnout detection algorithms
- [ ] Build notification systems

### Improvement Cycle Planning
Based on monitoring results:
- [ ] Prioritize richer metadata additions
- [ ] Design activity classification system
- [ ] Plan workflow discovery improvements
- [ ] Schedule performance optimizations

---

## Rollback Plan

If critical issues discovered during monitoring:

### Immediate Rollback
```bash
# Stop signal emitter
kill $(cat /tmp/signal_emitter.pid)

# Disable Jarvis feature
sudo nano /etc/systemd/system/focusdeck.service.d/override.conf
# Change: Environment="Features__Jarvis=false"

# Restart service
sudo systemctl daemon-reload
sudo systemctl restart focusdeck
```

### Data Preservation
```bash
# Backup PostgreSQL data
pg_dump -h localhost -U focusdeck_app focusdeck > \
  /tmp/focusdeck_rollback_backup_$(date +%Y%m%d_%H%M%S).sql

# Preserve activity signals for analysis
pg_dump -h localhost -U focusdeck_app focusdeck \
  -t ActivitySignals -t JarvisWorkflowRuns > \
  /tmp/jarvis_data_backup.sql
```

---

## Summary

### Current Blockers (Priority Order)

1. 🔴 **PostgreSQL databases not provisioned** → Task 1
2. 🔴 **Environment variables not configured** → Task 2
3. 🔴 **Database migrations not applied to PostgreSQL** → Task 3
4. 🟡 **Hangfire not configured for PostgreSQL** → Task 4
5. 🟡 **E2E validation not executed** → Task 6
6. 🟡 **24-hour monitoring not started** → Task 7

### Estimated Timeline

- **Task 1-2**: 1-2 hours (database provisioning + env vars)
- **Task 3-4**: 30-45 minutes (migrations + Hangfire)
- **Task 5**: 15 minutes (deployment verification)
- **Task 6**: 30-45 minutes (E2E testing)
- **Task 7**: 24 hours + 2-4 hours analysis

**Total**: ~2-3 hours setup + 24+ hours monitoring

### Dependencies

- Task 2 requires Task 1 (need databases before setting connection strings)
- Task 3 requires Task 2 (need connection strings to run migrations)
- Task 4 requires Task 3 (Hangfire schema needs database)
- Task 5 requires Tasks 1-4 (full infrastructure ready)
- Task 6 requires Task 5 (deployed system to test)
- Task 7 requires Task 6 (validated system to monitor)

### Phase 1.5 & Phase 4 R&D Status

**Status**: 🔴 ON HOLD  
**Blocker**: Infrastructure provisioning + 24-hour monitoring campaign  
**Resume Criteria**: All tasks 1-7 complete with successful validation

---

**Document Owner**: Operations/DevOps  
**Last Updated**: November 14, 2025  
**Next Review**: After Task 7 completion
