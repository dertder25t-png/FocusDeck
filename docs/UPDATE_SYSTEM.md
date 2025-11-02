# FocusDeck Server Update System

## Overview

FocusDeck includes a built-in update workflow designed for self-hosted Linux deployments. From the web dashboard you can check for new releases, trigger an update, and watch the server come back online without leaving the browser.

The update controller now exposes a single implementation that powers both the web UI and API consumers. This document explains how it works, how to configure it, and how to troubleshoot common issues.

## Feature Highlights

- **One-click updates** – pulls the latest code, compiles, and restarts the service.
- **GitHub awareness** – compares the current commit with `master` on GitHub.
- **Status reporting** – exposes update progress, configuration checks, and the most recent log entries.
- **Health monitoring** – lightweight ping endpoint keeps the UI aware of server availability.
- **Scriptable setup** – helper script configures sudo, logging, and environment defaults on fresh servers.

## API Endpoints

| Method | Path | Description |
| ------ | ---- | ----------- |
| `GET` | `/api/update/check-updates` | Returns information about the current and latest GitHub commits. |
| `POST` | `/api/update/trigger` | Starts the update process (Linux only). |
| `GET` | `/api/update/status` | Reports whether an update is running and last log entry. |
| `GET` | `/api/update/check-config` | Validates repository path, required tools, and permissions. |
| `GET` | `/api/server/check-updates` | Same as `update/check-updates` (kept for UI compatibility). |
| `POST` | `/api/server/update` | Delegates to the update trigger endpoint (legacy route). |
| `GET` | `/api/server/update-status` | Returns recent log lines and derived status. |
| `GET` | `/api/server/health` | Quick health probe used by the UI to detect restarts. |

All responses are JSON. Update-related endpoints return HTTP 400 when prerequisites are missing or an update is already running.

## Update Flow

1. User clicks **Check for Updates**.
2. UI calls `GET /api/update/check-updates` and compares the commits.
3. If an update is available, the user clicks **Update Server**.
4. UI sends `POST /api/update/trigger`.
5. Server runs:
   - `git pull origin master`
   - `dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release`
   - `sudo systemctl restart focusdeck`
6. UI polls `GET /api/server/health` every second. When it succeeds the page refreshes automatically.

The log for each run is written to `/var/log/focusdeck/update.log`.

## Requirements

- Linux host with `git`, `dotnet`, and `systemctl`.
- FocusDeck repository cloned locally (default `/home/focusdeck/FocusDeck`).
- FocusDeck service user (`focusdeck` by default) needs passwordless sudo for `systemctl restart focusdeck`.
- Static files served by the FocusDeck web server (no special reverse proxy rules required).

Set `FOCUSDECK_REPO` if the repository lives somewhere other than the default path.

## Quick Configuration Script

Run as root to prepare a fresh server:

```bash
sudo bash configure-update-system.sh
```

The script will:

1. Confirm or clone the repository location.
2. Fix permissions for the focusdeck user.
3. Set the `FOCUSDECK_REPO` environment variable in the systemd service (if needed).
4. Grant sudo access for restarting the service.
5. Create `/var/log/focusdeck` and apply sane permissions.
6. Reload systemd and restart the FocusDeck service.

## Viewing Logs

```
sudo tail -f /var/log/focusdeck/update.log
```

The UI also exposes the last ten lines through `GET /api/server/update-status`.

## Troubleshooting

| Symptom | Suggested Checks |
| ------- | ---------------- |
| Update button disabled | Confirm the server is running on Linux and `FOCUSDECK_REPO` is correct. |
| Update fails immediately | Run `sudo -u focusdeck git status` and `dotnet --version` to verify tools are installed and permissions valid. |
| Service does not restart | Check `journalctl -u focusdeck` for runtime errors. |
| UI never reloads | Ensure `/api/server/health` returns HTTP 200 and reverse proxies forward the correct scheme/headers. |
| GitHub rate limit | Add a token-backed proxy or reduce frequency of checks (GitHub allows 60 unauthenticated requests/hour). |

## Security Notes

- Protect the update endpoints with authentication for production deployments.
- Serve the dashboard over HTTPS.
- Keep `/var/log/focusdeck` readable only by the FocusDeck service account and administrators.
- Consider adding alerting on update failures or unusual log messages.

## Next Steps

- Add changelog visibility in the UI.
- Support scheduled or automatic updates (opt-in).
- Implement rollback and backup hooks.
- Extend support to Windows deployments.

_Last updated: November 2025_
