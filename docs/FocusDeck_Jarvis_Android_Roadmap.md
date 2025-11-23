# FocusDeck + Jarvis: Android/Mobile Roadmap (Deferred Track)

This document tracks the Android/Mobile (MAUI) work for FocusDeck + Jarvis.  
The main execution roadmap (`docs/FocusDeck_Jarvis_Execution_Roadmap.md`) focuses on:

- Phase 0–1: Linux web server/backend hardening
- Phase 0–1: Windows desktop (WPF) client

Android/Mobile should only start once the server and Windows client are in production and stable.

---

## Phase 0 — Baseline Mobile Setup (follow after Phase 0 server/desktop)

**Goal:** Ensure the MAUI client targets the right runtime and talks to the canonical dev API URL.

- [x] Align the MAUI project with the new stack by targeting .NET 9 in `src/FocusDeck.Mobile/FocusDeck.Mobile.csproj`.
- [x] Confirm the base API URL is `http://10.0.2.2:5000` for emulator and `http://<dev-machine>:5000` for physical devices.
- [x] Verify that once the backend is stable, the mobile client can hit `/healthz` and simple test endpoints without any code changes to server logic.

---

## Phase 1 — Auth + Tenant Context (Android/Mobile)

**Goal:** Mirror the Web/Desktop PAKE + tenancy experience on Android, reusing the same endpoints and contracts.

### 1.1 Authentication UI (Android/Mobile)

- [x] Provisioning page subscribes to tenant summary updates exposed by `IMobileAuthService`, so the active tenant name/slug appears on-device after login.
- [x] Implement provisioning + QR pairing (claim code → tokens) with `MobilePakeAuthService` + `ProvisioningPage`.
- [x] Wire the mobile provisioning view model to call the same `/v1/auth/pake` endpoints used by Web/Desktop and store tenant-scoped tokens.

**Files**

- `src/FocusDeck.Mobile/.../ProvisioningViewModel.cs`
- `src/FocusDeck.Mobile/Services/Auth/MobilePakeAuthService.cs`

### 1.2 UX pass (Android quick actions)

- [x] Implement login + "quick actions" (Start Note, Pair Device) on Android, similar to desktop quick actions.
- [x] Ensure minimal list screens exist (e.g., Notes list) to verify auth + API calls are working end-to-end.

**Files**

- `src/FocusDeck.Mobile/.../CommandDeckPage.xaml(.cs)`
- Related view models and navigation shell

---

## Phase 3 — Jarvis Signals (Android/Mobile)

**Goal:** Allow the Android client to receive Jarvis workflow updates and act on them.

- [ ] Implement the SignalR client for `NotificationsHub` so Android can receive `JarvisRunUpdated` notifications. (Current implementation uses raw WebSocket and does not handle `JarvisRunUpdated`).
- [ ] Map basic actions (show toast, start/pause, open URL, open note) to mobile UI elements.

**Files**

- `src/FocusDeck.Mobile/.../SignalR/**`
- Any platform-specific notification or navigation helpers

---

## Phase 5 — Device Agent (Android, optional)

**Goal:** Extend the device agent concept from Windows to Android (optional advanced phase).

- [ ] Define Android capabilities (e.g., open app, open note, show pill/notification) as skills.
- [ ] If needed, mirror the Windows agent job bundle and map a subset of skills to Android.

> This phase is optional and should come only after the Windows agent is stable.

---

## Acceptance Notes (Android/Mobile)

- Android app can login/register/pair via PAKE, using the same `/v1/auth/pake` endpoints as Web/Desktop.  
- Tenant context (name/slug) is visible in the mobile UI after login.  
- Quick actions (Start Note, Pair Device) work and hit the server successfully.  
- Jarvis workflow updates can be received on Android (if Phase 3 mobile work is completed).
