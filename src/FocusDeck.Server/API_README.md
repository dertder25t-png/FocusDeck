# FocusDeck Server - API Documentation

## Overview

FocusDeck Server provides a REST API with JWT authentication, refresh tokens, API versioning, and comprehensive observability features.

## Features

- **JWT Authentication with Refresh Tokens**: Secure token-based authentication
- **API Versioning (v1)**: Organized API endpoints with version support
- **CORS Support**: Configured for web, desktop (Tauri), and mobile (Capacitor/Ionic) clients
- **Health Checks**: Database and filesystem monitoring at `/v1/system/health`
- **Observability**: Serilog logging with correlation IDs, OpenTelemetry traces
- **Global Exception Handling**: Structured error responses with trace IDs

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQLite (default) or PostgreSQL (optional)

### Configuration

1. Copy `appsettings.Sample.json` to `appsettings.json`:
```bash
cp appsettings.Sample.json appsettings.json
```

2. Update the configuration values:

**JWT Settings:**
```json
{
  "Jwt": {
    "Key": "your-256-bit-secret-key-here-min-32-chars",
    "Issuer": "https://your-domain.com",
    "Audience": "focusdeck-clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Database:**
- **SQLite (Default)**: `"DefaultConnection": "Data Source=focusdeck.db"`
- **PostgreSQL**: `"DefaultConnection": "Host=localhost;Port=5432;Database=focusdeck;Username=postgres;Password=yourpassword"`

### Running the Server

```bash
dotnet run --project src/FocusDeck.Server
```

The server will start at:
- Development: `https://localhost:5239`
- Production: Configure your reverse proxy

## API Documentation

### Base URL

All API endpoints are versioned under `/v1/`:
- `https://your-domain.com/v1/...`

### Authentication

#### POST `/v1/auth/login`

Login and receive access + refresh tokens.

**Request:**
```json
{
  "username": "your-username",
  "password": "your-password"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "base64-encoded-refresh-token",
  "expiresIn": 3600
}
```

#### POST `/v1/auth/refresh`

Refresh your access token using a refresh token.

**Request:**
```json
{
  "accessToken": "expired-or-current-access-token",
  "refreshToken": "your-refresh-token"
}
```

**Response:**
```json
{
  "accessToken": "new-access-token",
  "refreshToken": "new-refresh-token",
  "expiresIn": 3600
}
```

#### POST `/v1/auth/revoke`

Revoke a refresh token (requires authentication).

**Request:**
```json
{
  "refreshToken": "token-to-revoke"
}
```

**Headers:**
```
Authorization: Bearer <your-access-token>
```

### Health Checks

#### GET `/healthz`

Simple health check (no authentication required).

**Response:**
```json
{
  "ok": true,
  "time": "2025-11-02T17:56:02.192Z"
}
```

#### GET `/v1/system/health`

Detailed health check with database and filesystem checks (no authentication required).

**Response:**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection successful",
      "duration": 45.2
    },
    {
      "name": "filesystem",
      "status": "Healthy",
      "description": "Write access verified for path: /data/assets",
      "duration": 12.5
    }
  ],
  "totalDuration": 57.7
}
```

### Protected Routes

All `/v1/*` routes (except `/v1/auth/*` and `/v1/system/health`) require authentication.

**Headers:**
```
Authorization: Bearer <your-access-token>
```

### Error Responses

All errors return structured JSON with trace ID:

```json
{
  "traceId": "00-abc123...",
  "code": "INVALID_ARGUMENT",
  "message": "Detailed error message"
}
```

**Error Codes:**
- `ARGUMENT_NULL`: Missing required parameter
- `INVALID_ARGUMENT`: Invalid parameter value
- `INVALID_OPERATION`: Operation not allowed in current state
- `UNAUTHORIZED`: Authentication required or failed
- `INTERNAL_ERROR`: Unexpected server error

## CORS Configuration

The server is configured to accept requests from:

### Web Clients
- `https://focusdeck.909436.xyz` (Production)
- `http://localhost:3000` (React/Vite dev)
- `http://localhost:5173` (Vite default)
- `http://localhost:5239` (Kestrel dev)

### Desktop Clients (Tauri)
- `tauri://localhost`
- `https://tauri.localhost`

### Mobile Clients (Capacitor/Ionic)
- `capacitor://localhost`
- `ionic://localhost`
- `http://localhost`

## Observability

### Logging

Serilog is configured with:
- Console output with structured logging
- Correlation IDs for request tracing
- Machine name and thread ID enrichment

Log format:
```
[17:56:02 INF] [correlation-id] Request logged with details
```

### OpenTelemetry

Traces are exported to console with:
- ASP.NET Core instrumentation
- Service name: "FocusDeck.Server"
- Activity/trace ID correlation

### Correlation IDs

Every request gets a correlation ID (`Activity.Current.Id` or `TraceIdentifier`) that appears in:
- Logs
- Error responses (as `traceId`)
- Response headers

## Database Migrations

The application uses Entity Framework Core. Migrations are applied automatically on startup.

For manual migrations:
```bash
dotnet ef migrations add MigrationName --project src/FocusDeck.Persistence
dotnet ef database update --project src/FocusDeck.Server
```

## Security Notes

1. **JWT Secret Key**: Must be at least 32 characters for HS256 algorithm
2. **HTTPS**: Always use HTTPS in production
3. **Refresh Tokens**: Stored in database with expiration and revocation support
4. **Token Rotation**: Old refresh tokens are revoked when new ones are issued
5. **CORS**: Update allowed origins for your deployment environment

## Production Deployment

1. Set environment to Production: `ASPNETCORE_ENVIRONMENT=Production`
2. Configure reverse proxy (nginx/Caddy) for HTTPS termination
3. Use PostgreSQL for production database
4. Set strong JWT secret key
5. Configure proper CORS origins
6. Enable forwarded headers for proxy (already configured for Cloudflare)

## Support

For issues or questions, please refer to the repository documentation or open an issue.
