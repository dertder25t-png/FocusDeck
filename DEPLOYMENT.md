# FocusDeck Server Production Deployment Guide

This guide provides instructions for deploying the FocusDeck server to a production environment.

## 1. Prerequisites

Before you begin, ensure you have the following installed on your production server:

- **.NET 8 SDK or later**
- **A production-ready database:**
  - **PostgreSQL (Recommended):** For robust, scalable production use.
  - **SQLite:** Suitable for smaller, single-instance deployments.
- **Redis (Optional):** For enhanced caching and performance.
- **A reverse proxy (e.g., Nginx, Apache):** To handle incoming traffic, SSL termination, and load balancing.

## 2. Configuration

The application is configured through `appsettings.Production.json` and environment variables. It's highly recommended to use environment variables for sensitive data.

### 2.1. Core Services

Set the connection strings for your database, Hangfire, and Redis.

**Using Environment Variables (Recommended):**

```bash
export ConnectionStrings__DefaultConnection="<Your-PostgreSQL-Connection-String>"
export ConnectionStrings__HangfireConnection="<Your-PostgreSQL-Connection-String>"
export ConnectionStrings__Redis="<Your-Redis-Connection-String>"
```

### 2.2. JWT (JSON Web Tokens)

For production, it's crucial to use a secure key store for your JWT signing keys. The application supports Azure Key Vault or environment variables. You must also configure the token `Issuer` and `Audience` to match your production domain.

**Option A: Azure Key Vault (Recommended)**

1.  Set up an Azure Key Vault instance.
2.  Configure your server's managed identity to have access to the Key Vault.
3.  Set the following environment variable:

    ```bash
    export Azure__KeyVault__VaultUrl="https://your-key-vault-name.vault.azure.net/"
    ```

**Option B: Environment Variables**

If you're not using Azure, you can provide the keys directly through environment variables. **Note:** This is less secure than using a dedicated key vault.

```bash
export JWT__SigningKey="<Your-Primary-Signing-Key>"
export JWT__FallbackSigningKey="<Your-Fallback-Signing-Key>"
```

**Issuer and Audience Configuration:**

```bash
export JWT__Issuer="https://your-production-domain.com"
export JWT__Audience="your-production-audience"
export JWT__AllowedIssuers__0="https://your-production-domain.com"
export JWT__AllowedIssuers__1="https://www.your-production-domain.com"
export JWT__AllowedAudiences__0="your-production-audience"
```

### 2.3. Google Authentication

To enable Google login, you'll need to create a project in the Google Cloud Console and obtain OAuth 2.0 credentials.

**Using Environment Variables (Recommended):**

```bash
export Google__ClientId="<Your-Google-ClientId>"
export Google__ClientSecret="<Your-Google-ClientSecret>"
```

### 2.4. CORS (Cross-Origin Resource Sharing)

Configure the allowed origins for your frontend application.

**Using Environment Variables (Recommended):**

```bash
export Cors__AllowedOrigins__0="https://your-frontend-domain.com"
export Cors__AllowedOrigins__1="https://another-allowed-domain.com"
```

## 3. Deployment

Follow these steps to build and run the application.

### 3.1. Build the Application

From the root of the repository, run the following command to build the server in release mode:

```bash
dotnet publish src/FocusDeck.Server -c Release -o ./publish
```

This will create a `publish` directory with the compiled application.

### 3.2. Run Database Migrations

Before starting the server, apply any pending database migrations:

```bash
cd ./publish
./FocusDeck.Server --apply-migrations
```

### 3.3. Run the Application

You can now start the application:

```bash
./FocusDeck.Server
```

It's recommended to run the application as a service (e.g., using `systemd` on Linux) to ensure it runs continuously and restarts on failure.

## 4. Verification

Once the application is running, you can verify its status using the health check endpoints.

-   **Basic Health Check:** `http://your-server-address/healthz`
-   **Detailed Health Check:** `http://your-server-address/v1/system/health`

These endpoints can be used with monitoring tools to ensure your application is running correctly.
