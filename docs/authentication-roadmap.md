# Authentication & Key Management Roadmap

## FocusDeck.Server

**Implemented (baseline reference)**
- [x] JWT bearer auth with token validation + revocation hook (`src/FocusDeck.Server/Program.cs:317`)
- [x] Token issuance + refresh helpers (`src/FocusDeck.Server/Services/Auth/TokenService.cs:8`)
- [x] Refresh token entity/config (e.g., `src/FocusDeck.Domain/Entities/Auth/RefreshToken.cs:1`)
- [x] SRP-6a registration/login handshake with server-side session cache (`src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs:17`, `src/FocusDeck.Server/Services/Auth/SrpSessionCache.cs:1`)
- [x] Pairing endpoints + pairing entity (`src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs:129`)
- [x] DB-backed access token revocation (`src/FocusDeck.Server/Services/Auth/AccessTokenRevocationService.cs:9`)
- [x] SignalR notifications hub with user grouping (`src/FocusDeck.Server/Hubs/NotificationsHub.cs:9`)

- **TODO**
- [x] Extended SRP implementation with Argon2id KDF and verifier metadata versioning.
- [x] Persist PAKE verifier/kdf metadata (algorithm version, params) alongside credentials (`src/FocusDeck.Domain/Entities/Auth/PakeCredential.cs:1`, `src/FocusDeck.Persistence/Migrations/20251106113000_AddPakeMetadata.cs:1`).
- [x] Device management API: list devices, revoke device, bind refresh tokens to device records (`src/FocusDeck.Server/Controllers/v1/AuthController.cs:55`, `src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs:95`).
- [x] Emit SignalR forced-logout events and client handling (`src/FocusDeck.Server/Services/Auth/AccessTokenRevocationService.cs:10`, `src/FocusDeck.Server/Controllers/v1/AuthController.cs:382`, `src/FocusDeck.Desktop/Services/Auth/KeyProvisioningService.cs:13`).
- [x] Integrate Redis-backed revocation cache + pub/sub for cross-node invalidation (`src/FocusDeck.Server/Services/Auth/AccessTokenRevocationService.cs:10`, `src/FocusDeck.Server/Program.cs:132`).
- [x] Extend vault storage with KDF parameter metadata + migrations (`src/FocusDeck.Domain/Entities/Auth/KeyVault.cs:1`, `src/FocusDeck.Persistence/Migrations/20251106121000_AddVaultMetadata.cs:1`, `src/FocusDeck.Desktop/Services/Auth/KeyProvisioningService.cs:80`).
- [x] Add health checks for Redis/auth subsystems (`src/FocusDeck.Server/Program.cs:240`, `src/FocusDeck.Server/HealthChecks/RedisHealthCheck.cs:1`).
- [x] Improve audit logging + rate limiting on failed PAKE attempts (`src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs:38`, `src/FocusDeck.Server/Services/Auth/AuthAttemptLimiter.cs:1`).

## FocusDeck.Shared (contracts + clients)

- [x] Client sync + device registration helpers (`src/FocusDeck.Shared/Sync/ClientSyncManager.cs:10`)
- [x] Shared SRP helpers + handshake DTOs (`src/FocusDeck.Shared/Security/Srp.cs:1`, `src/FocusDeck.Shared/Contracts/Auth/PakeContracts.cs:4`) - *Updated to support Argon2id KDF.*

**TODO**
- [x] Provide typed SignalR client contracts for forced logout + telemetry events (`src/FocusDeck.Shared/SignalR/Notifications/INotificationClientContract.cs:1`, `src/FocusDeck.Server/Hubs/NotificationsHub.cs:9`, `src/FocusDeck.Desktop/Services/RemoteControllerService.cs:15`).
- [x] Document encryption metadata schema versioning for clients (`docs/encryption-metadata.md`, `src/FocusDeck.Shared/Contracts/Auth/PakeContracts.cs:4`, `src/FocusDeck.Domain/Entities/Auth/KeyVault.cs:1`).

## FocusDeck.Desktop (WPF)

**Implemented**
- [x] Token store + API client bootstrap from persisted tokens (`src/FocusDeck.Desktop/Services/Auth/TokenStore.cs:9`, `src/FocusDeck.Desktop/App.xaml.cs:30`)
- [x] Key provisioning service handling login/register/pair/refresh (`src/FocusDeck.Desktop/Services/Auth/KeyProvisioningService.cs:9`)
- [x] SRP client handshake aligned with server (`src/FocusDeck.Desktop/Services/Auth/KeyProvisioningService.cs:24`)
- [x] Onboarding modal for pairing/login flows (`src/FocusDeck.Desktop/Views/OnboardingWindow.xaml.cs:9`)
- [x] Logout UI + onboarding relaunch (`src/FocusDeck.Desktop/Views/ShellWindow.xaml.cs:29`)

**TODO**
- [x] Integrate Argon2 KDF + metadata handling with EncryptionService.
  - *Server-side support for Argon2id is complete and ready for client integration.*
- [x] Secure key storage (DPAPI/Windows Hello) for master key.
- [x] QR provisioning UI (camera/one-time code) wired to pair/redeem endpoints.
- [x] SignalR forced-logout handling + local key wipe.
- [x] Local encrypted data store + FTS5 search. (FTS5 deferred)
- [x] Migration path from legacy JWT accounts.

## FocusDeck.Mobile (MAUI)

**Implemented**
- [x] Shared client sync manager usage (`src/FocusDeck.Shared/Sync/ClientSyncManager.cs:10`) *basis for device registration*.
- [x] SRP login/token issuance wired through `MobileTokenStore` into FocusDeck server sync endpoints (`src/FocusDeck.Mobile/Services/FocusDeckServerSyncService.cs:1`, `src/FocusDeck.Mobile/ViewModels/StudyTimerViewModel.cs:1`).
- [x] Argon2 vault export submitted during registration + pairing flows (`src/FocusDeck.Mobile/Services/Auth/MobilePakeAuthService.cs:1`, `src/FocusDeck.Mobile/ViewModels/CloudSettingsViewModel.cs:1`, `src/FocusDeck.Mobile/Services/DevicePairingService.cs:1`).

**TODO**
- [x] Integrate secure storage (Android Keystore / iOS Keychain) for vault keys.
- [x] Build QR scan + pairing UI, plus redeem flow.
- [x] Handle SignalR forced-logouts + local data wipe.
- [x] Local encrypted note store + search index.
- [x] Biometric / WebAuthn binding for key unlock.

## FocusDeck.Persistence / Domain

**Implemented**
- [x] Entities: `PakeCredential`, `KeyVault`, `PairingSession`, `RefreshToken`, `RevokedAccessToken`.

**TODO / Recent Changes**
- [x] Extend entities with KDF parameter metadata + device binding.
- [x] Add migrations for new metadata + Redis cache integration config.
  - Migration adds indexes for `RevokedAccessTokens` (`Jti` unique, `ExpiresUtc`) and `PairingSessions` (`UserId,Code,Status`, `Code`, `ExpiresAt`). See `src/FocusDeck.Persistence/Migrations/20251107204207_InitialAuthMigration.cs`.
  - Optional Redis connection string added to server config (`src/FocusDeck.Server/appsettings.Sample.json:36`).
- [x] Add indexes / TTL strategy for revocation + pairing tables.
  - Hourly pruning background service now deletes expired `RevokedAccessTokens`, `RefreshTokens`, and `PairingSessions` (`src/FocusDeck.Server/Services/Auth/TokenPruningService.cs:1`).

## Global / Cross-cutting

- [x] End-to-end tests: PAKE handshake, vault round-trip, forced logout propagation.
  - Added tests in `tests/FocusDeck.Server.Tests`:
    - `AuthPakeE2ETests.Pake_Register_Login_VaultRoundTrip` validates full SRP handshake and vault presence.
    - `ForcedLogoutPropagationTests.Revoke_Sends_ForceLogout_To_User_Group` validates SignalR forced logout broadcast.
  - Note: tests use in-memory SQLite and fake hub context; wire into CI as needed.
- [x] Crypto review + migration/versioning guidance.
  - KDFs: default Argon2id (2 threads, t=3, m=64MiB) with per-user salt; metadata preserved in `PakeCredential.KdfParametersJson` and `KeyVault.KdfMetadataJson`.
  - Cipher suite versioning: `KeyVault.CipherSuite` defaults to `AES-256-GCM` and `KeyVault.Version` tracks vault serialization. Clients should include both in exports and validate on import.
  - Migration strategy: support credential upgrade via `v1/auth/pake/upgrade`; rotate JWT signing keys with overlap. For DB cleanup, rely on TTL pruning service and indexes on `ExpiresUtc`/`ExpiresAt` for efficient deletes.
