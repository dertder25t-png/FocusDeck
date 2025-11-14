# OPERATIONS.md

## FocusDeck Server Operations Guide

This guide covers operational aspects of running the FocusDeck server in production.

## Table of Contents
- [Environment Configuration](#environment-configuration)
- [Database Management](#database-management)
- [Background Jobs (Hangfire)](#background-jobs-hangfire)
- [Logging](#logging)
- [Health Checks](#health-checks)
- [Security](#security)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

---

## Environment Configuration

### Required Environment Variables

For production deployment, use environment variables instead of `appsettings.json`:

```bash
# JWT Configuration (REQUIRED)
export Jwt__Key="your-secure-256-bit-key-minimum-32-chars"
export Jwt__Issuer="https://your-domain.com"
export Jwt__Audience="focusdeck-clients"
export Jwt__AccessTokenExpirationMinutes="60"
export Jwt__RefreshTokenExpirationDays="7"

# Database Connection
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=focusdeck;Username=postgres;Password=yourpassword"
export ConnectionStrings__HangfireConnection="Host=localhost;Port=5432;Database=focusdeck_jobs;Username=postgres;Password=yourpassword"

# Google OAuth (Optional)
export Authentication__Google__ClientId="your-client-id.apps.googleusercontent.com"
export Authentication__Google__ClientSecret="your-client-secret"

# CORS Configuration
export Cors__AllowedOrigins__0="https://your-domain.com"
export Cors__AllowedOrigins__1="focusdeck-desktop://app"
export Cors__AllowedOrigins__2="focusdeck-mobile://app"

# Storage Configuration
export Storage__Root="/data/assets"

# Logging
export Serilog__MinimumLevel__Default="Information"
export Serilog__MinimumLevel__Override__Microsoft.AspNetCore="Warning"

# ASP.NET Core
export ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_URLS="http://0.0.0.0:5000"
```

### Configuration Files

1. **appsettings.json** - Base configuration (development defaults)
2. **appsettings.Production.json** - Production overrides
3. **appsettings.Sample.json** - Template with documentation

**Never commit secrets to version control!**

---

## Database Management

### SQLite (Development)

Default database file: `focusdeck.db`

```bash
# View database
sqlite3 focusdeck.db

# Backup
cp focusdeck.db focusdeck.db.backup.$(date +%Y%m%d_%H%M%S)
```

### PostgreSQL (Production)

```bash
# Create database
createdb focusdeck
createdb focusdeck_jobs  # For Hangfire

# Backup
pg_dump focusdeck > focusdeck_backup_$(date +%Y%m%d_%H%M%S).sql

# Restore
psql focusdeck < focusdeck_backup_20250101.sql
```

### Migrations

The server automatically creates tables on startup using `EnsureCreated()`.

For production with existing data, consider using EF Core migrations:

```bash
# Create migration
cd src/FocusDeck.Server
dotnet ef migrations add InitialCreate --context AutomationDbContext

# Apply migrations
dotnet ef database update --context AutomationDbContext
```

---

## Background Jobs (Hangfire)

### Dashboard Access

Access the Hangfire dashboard at: `https://your-domain.com/hangfire`

**Note:** Dashboard requires authentication (JWT token).

### Job Types

1. **TranscribeLectureJob** - Transcribes audio lectures using Whisper
2. **SummarizeLectureJob** - Generates lecture summaries using LLM
3. **GenerateLectureNoteJob** - Creates structured study notes
4. **VerifyNoteJob** - Verifies note completeness and generates suggestions

### Monitoring Jobs

```bash
# View active jobs
curl -H "Authorization: Bearer YOUR_TOKEN" https://your-domain.com/hangfire/jobs/processing

# View failed jobs
curl -H "Authorization: Bearer YOUR_TOKEN" https://your-domain.com/hangfire/jobs/failed
```

### Job Configuration

Configure in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Host=localhost;Database=focusdeck_jobs;..."
  }
}
```

Hangfire worker configuration in `Program.cs`:

```csharp
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Number of concurrent job workers
    options.ServerName = $"FocusDeck-{Environment.MachineName}";
});
```

---

## Logging

### Serilog Configuration

Logs are written to:
- **Console** - Structured logs with correlation IDs
- **File** (optional) - Configure in `appsettings.json`

### Log Levels

- **Debug** - Detailed debugging information
- **Information** - General information messages
- **Warning** - Warnings and potential issues
- **Error** - Errors and exceptions
- **Fatal** - Critical failures

### Correlation IDs

Every request has a correlation ID for tracing:

```
[16:19:50 INF] [00-97fcc3cb62938ddac1b0c8c23593118b-a5241164db4c516e-01] HTTP GET /v1/system/health responded 200
```

### Viewing Logs

```bash
# Follow logs in real-time
journalctl -u focusdeck-server -f

# Filter by correlation ID
journalctl -u focusdeck-server | grep "97fcc3cb"

# Last 100 lines
journalctl -u focusdeck-server -n 100
```

---

## Health Checks

### Health Endpoint

**GET** `/v1/system/health`

**Response:**

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": null,
      "duration": 44.4587
    },
    {
      "name": "filesystem",
      "status": "Healthy",
      "description": null,
      "duration": 2.8279
    }
  ],
  "totalDuration": 51.4882
}
```

### Health Check Types

1. **database** - EF Core database connectivity check
2. **filesystem** - Storage directory write permissions

### Integration with Load Balancers

Configure your load balancer to poll `/v1/system/health`:

```nginx
# Nginx upstream health check
upstream focusdeck_backend {
    server 10.0.0.1:5000 max_fails=3 fail_timeout=30s;
    server 10.0.0.2:5000 max_fails=3 fail_timeout=30s;
}

location / {
    proxy_pass http://focusdeck_backend;
}
```

---

## Security

### JWT Token Security

1. **Key Rotation**
   - Rotate JWT signing keys regularly
   - Support multiple valid keys during rotation period
   - Store keys in secure vault (Azure Key Vault, AWS Secrets Manager)

2. **Token Expiration**
   - Access tokens: 60 minutes (configurable)
   - Refresh tokens: 7 days (configurable)

3. **Refresh Token Security**
   - Tokens are hashed using SHA-256
   - Client fingerprinting to detect token theft
   - Automatic family revocation on replay attack

### Google OAuth

To enable Google OAuth:

1. Go to https://console.cloud.google.com/
2. Create OAuth 2.0 credentials
3. Configure authorized redirect URIs
4. Set environment variables:

```bash
export Authentication__Google__ClientId="your-id.apps.googleusercontent.com"
export Authentication__Google__ClientSecret="your-secret"
```

### CORS Policy

Strict CORS policy with explicit allow-list:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-domain.com",
      "focusdeck-desktop://app",
      "focusdeck-mobile://app"
    ]
  }
}
```

### HTTPS / TLS

**CRITICAL:** Always use HTTPS in production.

When behind a reverse proxy (recommended):

```csharp
// Program.cs handles forwarded headers from Cloudflare/Nginx
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

---

## Monitoring

### OpenTelemetry

The server exports OpenTelemetry traces:

- **HTTP requests** - AspNetCore instrumentation
- **Database queries** - EF Core instrumentation
- **SignalR** - Real-time connection tracing

Configure exporters in `Program.cs`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter(); // Change to OTLP for production
    });
```

### Metrics to Monitor

1. **Request Rate** - Requests per second
2. **Response Time** - p50, p95, p99 latencies
3. **Error Rate** - 5xx errors per minute
4. **Database Connection Pool** - Active connections
5. **Hangfire Queue** - Pending/failed jobs
6. **Asset Storage** - Disk usage
7. **Memory Usage** - Heap size, GC pressure

### Jarvis Operations

For details, see `docs/JARVIS_IMPLEMENTATION_ROADMAP.md` (“Jarvis Operations” section).

- Monitor `FocusDeck.Jarvis` metrics:
  - `jarvis.runs.started`, `jarvis.runs.succeeded`, `jarvis.runs.failed`
  - `jarvis.runs.duration.seconds` (watch for long-running or stuck runs)
- Watch logs from `JarvisWorkflowJob` for frequent `Failed` transitions or SignalR delivery warnings.
- Check for sustained HTTP 429 responses from `/v1/jarvis/run-workflow` as an indicator that per-user Jarvis run concurrency limits are being hit.
- Remember that `Features:Jarvis` controls API + `/jarvis` UI exposure; enable it only for canary environments/tenants until metrics look healthy.

---

## Troubleshooting

### Common Issues

#### 1. Database Connection Failed

```
Failed to connect to database
```

**Solution:**
- Check connection string in environment variables
- Verify database server is running
- Check firewall rules

```bash
# Test PostgreSQL connection
psql -h localhost -U postgres -d focusdeck
```

#### 2. JWT Key Too Short

```
JWT:Key must be configured with at least 32 characters in production
```

**Solution:**
```bash
# Generate secure key
openssl rand -base64 32 | tr -d '\n'
export Jwt__Key="<generated-key>"
```

#### 3. Filesystem Health Check Failed

```
Health check filesystem with status Unhealthy: Parent directory does not exist: /data
```

**Solution:**
```bash
# Create storage directory
sudo mkdir -p /data/assets
sudo chown focusdeck:focusdeck /data/assets
```

#### 4. Hangfire Not Starting

**Symptoms:** Jobs not processing, dashboard not available

**Solution:**
- Ensure PostgreSQL is used (Hangfire requires PostgreSQL)
- Check `ConnectionStrings:HangfireConnection`
- Verify database permissions

#### 5. SignalR Connection Failed

```
WebSocket connection failed
```

**Solution:**
- Check WebSocket support in reverse proxy:

```nginx
# Nginx WebSocket configuration
location /hubs/ {
    proxy_pass http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
}
```

### Debug Mode

Enable detailed logging:

```bash
export Serilog__MinimumLevel__Default="Debug"
export Serilog__MinimumLevel__Override__Microsoft="Debug"
```

### Performance Issues

1. **Slow database queries** - Enable EF Core query logging
2. **High memory usage** - Review Hangfire worker count
3. **Slow file uploads** - Check disk I/O and network bandwidth

---

## Backup Strategy

### What to Backup

1. **Database** - All user data, lectures, notes, sessions
2. **Asset Files** - Uploaded audio/image files in `/data/assets`
3. **Configuration** - Environment-specific settings

### Automated Backups

```bash
#!/bin/bash
# backup-focusdeck.sh

BACKUP_DIR="/backups/focusdeck"
DATE=$(date +%Y%m%d_%H%M%S)

# Backup PostgreSQL
pg_dump focusdeck > "$BACKUP_DIR/db_$DATE.sql"

# Backup asset files
tar -czf "$BACKUP_DIR/assets_$DATE.tar.gz" /data/assets

# Keep last 30 days
find "$BACKUP_DIR" -name "*.sql" -mtime +30 -delete
find "$BACKUP_DIR" -name "*.tar.gz" -mtime +30 -delete
```

### Restore Process

```bash
# Restore database
psql focusdeck < /backups/focusdeck/db_20250101_120000.sql

# Restore assets
tar -xzf /backups/focusdeck/assets_20250101_120000.tar.gz -C /
```

---

## Cloudflare Tunnel

### Setup

1. Install cloudflared:
```bash
curl -L --output cloudflared.deb https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared.deb
```

2. Authenticate:
```bash
cloudflared tunnel login
```

3. Create tunnel:
```bash
cloudflared tunnel create focusdeck
```

4. Configure tunnel (`~/.cloudflared/config.yml`):
```yaml
tunnel: <tunnel-id>
credentials-file: /home/user/.cloudflared/<tunnel-id>.json

ingress:
  - hostname: focusdeck.your-domain.com
    service: http://localhost:5000
  - service: http_status:404
```

5. Run tunnel:
```bash
cloudflared tunnel run focusdeck
```

### Systemd Service

```ini
# /etc/systemd/system/cloudflared.service
[Unit]
Description=Cloudflare Tunnel
After=network.target

[Service]
Type=simple
User=focusdeck
ExecStart=/usr/local/bin/cloudflared tunnel --no-autoupdate run focusdeck
Restart=on-failure
RestartSec=5s

[Install]
WantedBy=multi-user.target
```

---

## Performance Tuning

### Database Optimization

```sql
-- Add indexes for common queries
CREATE INDEX idx_lectures_recorded_at ON "Lectures" ("RecordedAt");
CREATE INDEX idx_lectures_status ON "Lectures" ("Status");
CREATE INDEX idx_assets_uploaded_at ON "Assets" ("UploadedAt");
```

### Caching

Consider adding Redis for:
- Lecture summaries
- User sessions
- Frequently accessed assets

### Horizontal Scaling

1. **Database** - Use PostgreSQL with read replicas
2. **API Servers** - Run multiple instances behind load balancer
3. **Asset Storage** - Use object storage (S3, Azure Blob)
4. **Hangfire** - Distribute workers across servers

---

## Support and Resources

- **GitHub Issues**: https://github.com/dertder25t-png/FocusDeck/issues
- **Documentation**: See `/docs` directory
- **API Reference**: `https://your-domain.com/swagger`

---

Last Updated: January 2025
