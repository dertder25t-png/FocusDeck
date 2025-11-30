using FocusDeck.Server.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using Asp.Versioning;
using System.Numerics;
using FocusDeck.Domain.Entities.Auth;
using FocusDeck.Persistence;
using FocusDeck.Shared.Contracts.Auth;
using FocusDeck.Shared.Security;
using FocusDeck.Domain.Entities.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using FocusDeck.Server.Services.Auth;
using FocusDeck.Server.Services.Tenancy;
using System.Threading.Tasks;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth/pake")]
[EnableRateLimiting("AuthBurst")]
public class AuthPakeController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<AuthPakeController> _logger;
    private readonly FocusDeck.Server.Services.Auth.ITokenService _tokenService;
    private readonly FocusDeck.Server.Services.Auth.ISrpSessionCache _srpSessions;
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _jwtSettings;
    private readonly IAuthAttemptLimiter _authLimiter;
    private readonly ITenantMembershipService _tenantMembership;

    public AuthPakeController(
        AutomationDbContext db,
        ILogger<AuthPakeController> logger,
        FocusDeck.Server.Services.Auth.ITokenService tokenService,
        FocusDeck.Server.Services.Auth.ISrpSessionCache srpSessions,
        IConfiguration configuration,
        JwtSettings jwtSettings,
        IAuthAttemptLimiter authLimiter,
        ITenantMembershipService tenantMembership)
    {
        _db = db;
        _logger = logger;
        _tokenService = tokenService;
        _srpSessions = srpSessions;
        _configuration = configuration;
        _jwtSettings = jwtSettings;
        _authLimiter = authLimiter;
        _tenantMembership = tenantMembership;
    }

    [HttpPost("register/start")]
    [AllowAnonymous]
    public IActionResult RegisterStart([FromBody] RegisterStartRequest request)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var maskedUser = AuthTelemetry.MaskIdentifier(request.UserId);
        _logger.LogInformation("PAKE register start for {UserId} (platform={Platform}) from {RemoteIp}", maskedUser, request.DevicePlatform ?? "unknown", remoteIp);

        if (string.IsNullOrWhiteSpace(request.UserId)) return BadRequest(new { error = "UserId required" });

        // Use Argon2id for all clients, including web (WASM supported)
        var kdfParameters = Srp.GenerateKdfParameters();
        var kdfParametersJson = JsonSerializer.Serialize(kdfParameters);

        return Ok(new RegisterStartResponse(kdfParametersJson, Srp.Algorithm, Srp.ModulusHex, (int)Srp.G));
    }

    [HttpPost("register/finish")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterFinish([FromBody] RegisterFinishRequest request)
    {
        try
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.VerifierBase64) || string.IsNullOrWhiteSpace(request.KdfParametersJson))
            {
                await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, false, "Missing fields");
                TrackRegisterFailure("missing-fields", request.UserId, null, remoteIp);
                return BadRequest(new { error = "Missing fields" });
            }

            // Normalize email to lowercase for case-insensitive matching
            var normalizedUserId = request.UserId.Trim().ToLowerInvariant();

            var existing = await _db.PakeCredentials.FirstOrDefaultAsync(c => c.UserId.ToLower() == normalizedUserId);
            if (existing != null)
            {
                await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, false, "User already registered");
                TrackRegisterFailure("already-registered", request.UserId, null, remoteIp);
                return Conflict(new { error = "User already registered" });
            }

            byte[] verifierBytes;
            try
            {
                verifierBytes = Convert.FromBase64String(request.VerifierBase64);
            }
            catch
            {
                await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, false, "Invalid verifier encoding");
                TrackRegisterFailure("invalid-verifier-encoding", request.UserId, null, remoteIp);
                return BadRequest(new { error = "Invalid verifier encoding" });
            }

            var verifier = Srp.FromBigEndian(verifierBytes);
            if (verifier.Sign <= 0 || verifier >= Srp.N)
            {
                await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, false, "Invalid verifier");
                TrackRegisterFailure("invalid-verifier", request.UserId, null, remoteIp);
                return BadRequest(new { error = "Invalid verifier" });
            }

            SrpKdfParameters? kdfParams = null;
            try
            {
                kdfParams = JsonSerializer.Deserialize<SrpKdfParameters>(request.KdfParametersJson);
            }
            catch (Exception ex)
            {
                await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, false, "Failed to parse KDF parameters");
                TrackRegisterFailure("invalid-kdf-json", request.UserId, null, remoteIp);
                _logger.LogWarning(ex, "Failed to parse KDF parameters for user {UserId}", request.UserId);
                return BadRequest(new { error = "Invalid KDF parameters JSON" });
            }

            if (kdfParams == null)
            {
                await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, false, "Invalid KDF parameters");
                TrackRegisterFailure("invalid-kdf", request.UserId, null, remoteIp);
                return BadRequest(new { error = "Invalid KDF parameters" });
            }

            if (string.IsNullOrWhiteSpace(kdfParams.SaltBase64))
            {
                await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, false, "Missing KDF salt");
                TrackRegisterFailure("missing-kdf-salt", request.UserId, null, remoteIp);
                return BadRequest(new { error = "Missing salt in KDF parameters" });
            }

            var tenantId = Guid.NewGuid();
            
            _db.PakeCredentials.Add(new PakeCredential
            {
                UserId = normalizedUserId,  // Store normalized (lowercase) email
                SaltBase64 = kdfParams.SaltBase64,
                VerifierBase64 = Convert.ToBase64String(Srp.ToBigEndian(verifier)),
                Algorithm = Srp.Algorithm,
                ModulusHex = Srp.ModulusHex,
                Generator = (int)Srp.G,
                KdfParametersJson = request.KdfParametersJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TenantId = tenantId
            });

            if (!string.IsNullOrWhiteSpace(request.VaultDataBase64))
            {
                _db.KeyVaults.Add(new KeyVault
                {
                    UserId = normalizedUserId,  // Store normalized (lowercase) email
                    VaultDataBase64 = request.VaultDataBase64,
                    Version = 1,
                    CipherSuite = request.VaultCipherSuite ?? "AES-256-GCM",
                    KdfMetadataJson = request.VaultKdfMetadataJson,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TenantId = tenantId
                });
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogWarning(dbEx, "Race condition detected in PAKE registration for user {UserId}", request.UserId);
                // Assume unique constraint violation on UserId
                return Conflict(new { error = "User already registered" });
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Database SaveChangesAsync failed for user {UserId}. InnerException: {InnerMessage}", request.UserId, dbEx.InnerException?.Message);
                throw; // Re-throw to be caught by outer catch block
            }
            
            await LogAuthEventAsync("PAKE_REGISTER_FINISH", request.UserId, true, deviceName: null, deviceId: null,
                metadataJson: request.KdfParametersJson);
            TrackRegisterSuccess(request.UserId, !string.IsNullOrWhiteSpace(request.VaultDataBase64), remoteIp);
            return Ok(new RegisterFinishResponse(true));
        }
        catch (Exception ex)
        {
            var userId = request?.UserId;
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogError(ex, "PAKE register finish unhandled exception for {UserId} from {RemoteIp}: {Message} | StackTrace: {StackTrace}", 
                userId, remoteIp, ex.Message, ex.StackTrace);
            await LogAuthEventAsync("PAKE_REGISTER_FINISH", userId, false, "Exception", metadataJson: JsonSerializer.Serialize(new { 
                exception = ex.Message ?? "unknown",
                exceptionType = ex.GetType().Name,
                stackTrace = ex.StackTrace
            }));
            TrackRegisterFailure("exception", userId, null, remoteIp);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("login/start")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginStart([FromBody] LoginStartRequest request)
    {
        try
        {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var maskedUser = AuthTelemetry.MaskIdentifier(request.UserId);
        _logger.LogInformation("PAKE login start for {UserId} (client={ClientId}) from {Platform} @ {RemoteIp}", maskedUser, request.ClientId ?? "unknown", request.DevicePlatform ?? "unknown", remoteIp);

        // Normalize email to lowercase for case-insensitive matching
        var normalizedUserId = request.UserId?.Trim().ToLowerInvariant() ?? string.Empty;
        var cred = await _db.PakeCredentials.AsNoTracking().FirstOrDefaultAsync(c => c.UserId.ToLower() == normalizedUserId);

        if (await _authLimiter.IsBlockedAsync(request.UserId, remoteIp))
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Too many failed attempts", request.ClientId, request.DeviceName);
            TrackLoginFailure("blocked", request.UserId, request.ClientId, request.DevicePlatform);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many attempts. Try again later." });
        }

        if (cred == null)
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "User not found", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("user-not-found", request.UserId, request.ClientId, request.DevicePlatform);
            return NotFound(new { error = "User not found" });
        }

        if (!string.Equals(cred.Algorithm, Srp.Algorithm, StringComparison.Ordinal))
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Unsupported algorithm", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("unsupported-algorithm", request.UserId, request.ClientId, request.DevicePlatform);
            return BadRequest(new { error = "Unsupported credential algorithm" });
        }

        var modulusHex = string.IsNullOrWhiteSpace(cred.ModulusHex) ? Srp.ModulusHex : cred.ModulusHex;
        if (!string.Equals(modulusHex, Srp.ModulusHex, StringComparison.OrdinalIgnoreCase) || cred.Generator != (int)Srp.G)
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Unsupported SRP parameters", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("unsupported-parameters", request.UserId, request.ClientId, request.DevicePlatform);
            return BadRequest(new { error = "Unsupported SRP parameters" });
        }

        BigInteger clientPublic;
        try
        {
            clientPublic = Srp.FromBigEndian(Convert.FromBase64String(request.ClientPublicEphemeralBase64));
        }
        catch
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Invalid client ephemeral", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("invalid-ephemeral", request.UserId, request.ClientId, request.DevicePlatform);
            return BadRequest(new { error = "Invalid client ephemeral" });
        }

        if (!Srp.IsValidPublicEphemeral(clientPublic))
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Invalid client ephemeral", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("invalid-ephemeral", request.UserId, request.ClientId, request.DevicePlatform);
            return BadRequest(new { error = "Invalid client ephemeral" });
        }

        var kdfParametersJson = NormalizeKdfParameters(cred);

        // Some historical credentials may not have a salt stored; attempt to read from
        // the KDF metadata first and fail gracefully if missing. This prevents a
        // FormatException and avoids returning HTTP 500 to clients.
        string? saltBase64 = cred.SaltBase64;
        if (string.IsNullOrWhiteSpace(saltBase64))
        {
            var parsedKdf = TryParseKdf(cred.KdfParametersJson);
            saltBase64 = parsedKdf?.SaltBase64;
        }

        if (string.IsNullOrWhiteSpace(saltBase64))
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Missing KDF salt", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("missing-salt", request.UserId, request.ClientId, request.DevicePlatform);
            return BadRequest(new { error = "Missing KDF salt" });
        }

        byte[] saltBytes;
        try
        {
            saltBytes = Convert.FromBase64String(saltBase64);
        }
        catch (Exception ex)
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Invalid KDF salt", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("invalid-salt", request.UserId, request.ClientId, request.DevicePlatform);
            _logger.LogWarning(ex, "Invalid KDF salt for user {UserId}", request.UserId);
            return BadRequest(new { error = "Invalid KDF salt" });
        }
        BigInteger verifier;
        try
        {
            if (string.IsNullOrWhiteSpace(cred.VerifierBase64))
            {
                 await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Missing credential verifier", request.ClientId, request.DeviceName);
                 await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
                 TrackLoginFailure("missing-verifier", request.UserId, request.ClientId, request.DevicePlatform);
                 return BadRequest(new { error = "Missing credential verifier" });
            }
            verifier = Srp.FromBigEndian(Convert.FromBase64String(cred.VerifierBase64));
        }
        catch (Exception ex)
        {
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Invalid credential verifier", request.ClientId, request.DeviceName);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            TrackLoginFailure("invalid-verifier", request.UserId, request.ClientId, request.DevicePlatform);
            _logger.LogWarning(ex, "Invalid credential verifier for user {UserId}", request.UserId);
            return BadRequest(new { error = "Invalid credential verifier" });
        }

        BigInteger serverSecret;
        BigInteger serverPublic;
        BigInteger scramble;
        var attempts = 0;
        do
        {
            (serverSecret, serverPublic) = Srp.GenerateServerEphemeral(verifier);
            scramble = Srp.ComputeScramble(clientPublic, serverPublic);
            attempts++;
            if (attempts > 3 && scramble.Sign == 0)
            {
                await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "SRP scramble zero", request.ClientId, request.DeviceName);
                await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
                TrackLoginFailure("scramble-zero", request.UserId, request.ClientId, request.DevicePlatform);
                return BadRequest(new { error = "SRP negotiation failed" });
            }
        } while (scramble.Sign == 0);

        var session = _srpSessions.Store(
            cred.UserId,
            saltBytes,
            verifier,
            clientPublic,
            serverSecret,
            serverPublic,
            scramble,
            request.ClientId,
            request.DeviceName,
            request.DevicePlatform);

        // Use the computed/normalized salt for compatibility: prefer the salt that was
        // derived from KDF metadata when the stored `SaltBase64` is empty.
        var returnedSaltBase64 = saltBase64 ?? cred.SaltBase64;

        return Ok(new LoginStartResponse(
            kdfParametersJson, // Null for legacy users, populated for modern users
            returnedSaltBase64!, // Prefer derived/normalized salt where present
            Convert.ToBase64String(Srp.ToBigEndian(serverPublic)),
            session.SessionId,
            cred.Algorithm,
            modulusHex,
            cred.Generator));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PAKE login start unhandled exception for {UserId}", request.UserId);
            await _authLimiter.RecordFailureAsync(request.UserId, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            await LogAuthEventAsync("PAKE_LOGIN_START", request.UserId, false, "Exception", request.ClientId, request.DeviceName, JsonSerializer.Serialize(new { exception = ex.Message }));
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("login/finish")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginFinish([FromBody] LoginFinishRequest request)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (await _authLimiter.IsBlockedAsync(request.UserId, remoteIp))
        {
            await LogAuthEventAsync("PAKE_LOGIN_FINISH", request.UserId, false, "Too many failed attempts", request.ClientId, request.DeviceName);
            TrackLoginFailure("blocked", request.UserId, request.ClientId, request.DevicePlatform);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many attempts. Try again later." });
        }

        if (!_srpSessions.TryGet(request.SessionId, out var session) || session == null)
        {
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            await LogAuthEventAsync("PAKE_LOGIN_FINISH", request.UserId, false, "Session expired", request.ClientId, request.DeviceName);
            TrackLoginFailure("session-expired", request.UserId, request.ClientId, request.DevicePlatform);
            return Unauthorized(new { error = "Session expired" });
        }

        if (!string.Equals(session.UserId, request.UserId, StringComparison.Ordinal))
        {
            _srpSessions.Remove(request.SessionId);
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            await LogAuthEventAsync("PAKE_LOGIN_FINISH", request.UserId, false, "Session user mismatch", request.ClientId, request.DeviceName);
            TrackLoginFailure("session-user-mismatch", request.UserId, request.ClientId, request.DevicePlatform);
            return Unauthorized(new { error = "Invalid session" });
        }

        try
        {
            var providedProof = Convert.FromBase64String(request.ClientProofBase64);
            var serverSessionSecret = Srp.ComputeServerSession(session.ClientPublic, session.Verifier, session.ServerSecret, session.Scramble);
            var sessionKey = Srp.ComputeSessionKey(serverSessionSecret);
            var expectedProof = Srp.ComputeClientProof(session.ClientPublic, session.ServerPublic, sessionKey);

            var ok = CryptographicOperations.FixedTimeEquals(expectedProof, providedProof);
            if (!ok)
            {
                _srpSessions.Remove(request.SessionId);
                await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
                await LogAuthEventAsync("PAKE_LOGIN_FINISH", request.UserId, false, "Invalid proof", request.ClientId, request.DeviceName);
                TrackLoginFailure("invalid-proof", request.UserId, request.ClientId, request.DevicePlatform);
                return Unauthorized(new { error = "Invalid proof" });
            }

            var serverProof = Srp.ComputeServerProof(session.ClientPublic, expectedProof, sessionKey);

            var roles = new[] { "User" };
            var tenantId = await _tenantMembership.EnsureTenantAsync(session.UserId, session.UserId, session.UserId, HttpContext.RequestAborted);
            var accessToken = await _tokenService.GenerateAccessTokenAsync(session.UserId, roles, tenantId, HttpContext.RequestAborted);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var deviceId = request.ClientId ?? session.ClientId ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-device";
            var deviceName = request.DeviceName ?? session.DeviceName ?? deviceId ?? "unknown";
            var devicePlatform = request.DevicePlatform ?? session.DevicePlatform;

            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var clientFingerprint = _tokenService.ComputeClientFingerprint(deviceId, userAgent);

            var refreshEntity = new FocusDeck.Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = session.UserId,
                TokenHash = _tokenService.ComputeTokenHash(refreshToken),
                ClientFingerprint = clientFingerprint,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                DeviceId = deviceId,
                DeviceName = deviceName,
                DevicePlatform = devicePlatform,
                LastAccessUtc = DateTime.UtcNow,
                TenantId = tenantId
            };
            _db.RefreshTokens.Add(refreshEntity);

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                await UpsertDeviceRegistrationAsync(session.UserId, tenantId, deviceId!, deviceName, devicePlatform);
            }

            await _db.SaveChangesAsync();

            var vault = await _db.KeyVaults.FindAsync(session.UserId);

            _srpSessions.Remove(request.SessionId);

            var metadata = new
            {
                deviceId,
                deviceName,
                devicePlatform,
                vaultCipherSuite = vault?.CipherSuite
            };

            await LogAuthEventAsync(
                "PAKE_LOGIN_FINISH",
                session.UserId,
                true,
                deviceId: deviceId,
                deviceName: deviceName,
                metadataJson: JsonSerializer.Serialize(metadata));

            await _authLimiter.ResetAsync(session.UserId, remoteIp);

            TrackLoginSuccess(session.UserId, tenantId, deviceId);
            return Ok(new LoginFinishResponse(
                true,
                vault != null,
                accessToken,
                refreshToken,
                _jwtSettings.AccessTokenExpirationMinutes * 60,
                Convert.ToBase64String(serverProof)));
        }
        catch (Exception ex)
        {
            _srpSessions.Remove(request.SessionId);
            _logger.LogWarning(ex, "SRP login finish failed");
            await _authLimiter.RecordFailureAsync(request.UserId, remoteIp);
            await LogAuthEventAsync("PAKE_LOGIN_FINISH", request.UserId, false, "Exception", request.ClientId, request.DeviceName, metadataJson: JsonSerializer.Serialize(new { exception = ex.Message }));
            TrackLoginFailure("exception", request.UserId, request.ClientId, request.DevicePlatform);
            return Unauthorized(new { error = "Invalid proof" });
        }
    }

    [HttpPost("upgrade")]
    [Authorize]
    public async Task<IActionResult> UpgradeCredential([FromBody] UpgradeCredentialRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !userId.Equals(request.UserId, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        // Use case-insensitive lookup
        var normalizedUserId = userId.Trim().ToLowerInvariant();
        var cred = await _db.PakeCredentials.FirstOrDefaultAsync(c => c.UserId.ToLower() == normalizedUserId);
        if (cred == null)
        {
            return NotFound(new { error = "User credential not found" });
        }

        var kdfParams = JsonSerializer.Deserialize<SrpKdfParameters>(request.KdfParametersJson);
        if (kdfParams == null)
        {
            return BadRequest(new { error = "Invalid KDF parameters" });
        }

        cred.VerifierBase64 = request.VerifierBase64;
        cred.KdfParametersJson = request.KdfParametersJson;
        cred.SaltBase64 = kdfParams.SaltBase64;
        cred.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await LogAuthEventAsync("PAKE_UPGRADE", userId, true, metadataJson: request.KdfParametersJson);

        return Ok(new UpgradeCredentialResponse(true));
    }


    // QR pairing endpoints
    [HttpPost("pair/start")]
    [Authorize]
    public async Task<IActionResult> PairStart([FromBody] PairStartRequest request)
    {
        var userId = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        string code = new Random().Next(100000, 999999).ToString();
        var session = new PairingSession
        {
            UserId = userId,
            Code = code,
            SourceDeviceId = request.SourceDeviceId,
            Status = PairingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        _db.PairingSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(new { pairingId = session.Id, code });
    }

    [HttpPost("pair/transfer")]
    [Authorize]
    public async Task<IActionResult> PairTransfer([FromBody] PairTransferRequest request)
    {
        var userId = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var session = await _db.PairingSessions.FirstOrDefaultAsync(p => p.Id == request.PairingId && p.UserId == userId);
        if (session == null || session.ExpiresAt < DateTime.UtcNow) return NotFound(new { error = "Pairing not found or expired" });

        session.VaultDataBase64 = request.VaultDataBase64;
        session.VaultKdfMetadataJson = request.VaultKdfMetadataJson;
        session.VaultCipherSuite = request.VaultCipherSuite;
        session.Status = PairingStatus.Ready;
        session.TargetDeviceId = request.TargetDeviceId;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost("pair/redeem")]
    [AllowAnonymous]
    public async Task<IActionResult> PairRedeem([FromBody] PairRedeemRequest request)
    {
        var session = await _db.PairingSessions.FirstOrDefaultAsync(p => p.Id == request.PairingId && p.Code == request.Code);
        if (session == null || session.ExpiresAt < DateTime.UtcNow) return NotFound(new { error = "Pairing not found or expired" });

        if (session.Status != PairingStatus.Ready || string.IsNullOrEmpty(session.VaultDataBase64))
            return BadRequest(new { error = "Not ready" });

        var roles = new[] { "User" };
        var accessToken = await _tokenService.GenerateAccessTokenAsync(session.UserId, roles, session.TenantId, HttpContext.RequestAborted);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Register the new refresh token (and device)
        var deviceId = session.TargetDeviceId ?? "mobile-pairing-" + session.Id.ToString()[..8];
        var deviceName = "Mobile Device";
        var devicePlatform = DevicePlatform.Android.ToString(); // Assume Android since this flow is primarily for it

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var clientFingerprint = _tokenService.ComputeClientFingerprint(deviceId, userAgent);

        var refreshEntity = new FocusDeck.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            TokenHash = _tokenService.ComputeTokenHash(refreshToken),
            ClientFingerprint = clientFingerprint,
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            DeviceId = deviceId,
            DeviceName = deviceName,
            DevicePlatform = devicePlatform,
            LastAccessUtc = DateTime.UtcNow,
            TenantId = session.TenantId
        };
        _db.RefreshTokens.Add(refreshEntity);

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            await UpsertDeviceRegistrationAsync(session.UserId, session.TenantId, deviceId, deviceName, devicePlatform);
        }

        session.Status = PairingStatus.Completed;
        await _db.SaveChangesAsync();

        await LogAuthEventAsync("PAKE_PAIR_REDEEM", session.UserId, true, deviceId: deviceId);
        TrackLoginSuccess(session.UserId, session.TenantId, deviceId);

        return Ok(new
        {
            vaultDataBase64 = session.VaultDataBase64,
            userId = session.UserId,
            vaultKdfMetadataJson = session.VaultKdfMetadataJson,
            vaultCipherSuite = session.VaultCipherSuite,
            accessToken,
            refreshToken,
            expiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60
        });
    }

    private async Task UpsertDeviceRegistrationAsync(string userId, Guid tenantId, string deviceId, string? deviceName, string? devicePlatform)
    {
        deviceId = deviceId.Trim();
        var registration = await _db.DeviceRegistrations.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);
        var platform = ParsePlatform(devicePlatform);

        if (registration == null)
        {
            registration = new DeviceRegistration
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceId = deviceId,
                DeviceName = string.IsNullOrWhiteSpace(deviceName) ? deviceId : deviceName!,
                Platform = platform,
                RegisteredAt = DateTime.UtcNow,
                LastSyncAt = DateTime.UtcNow,
                IsActive = true,
                TenantId = tenantId
            };
            _db.DeviceRegistrations.Add(registration);
        }
        else
        {
            registration.DeviceName = string.IsNullOrWhiteSpace(deviceName) ? registration.DeviceName : deviceName!;
            registration.Platform = platform;
            registration.LastSyncAt = DateTime.UtcNow;
            registration.IsActive = true;
            if (registration.TenantId == Guid.Empty)
            {
                registration.TenantId = tenantId;
            }
        }
    }

    private static DevicePlatform ParsePlatform(string? devicePlatform)
    {
        if (!string.IsNullOrWhiteSpace(devicePlatform) && Enum.TryParse<DevicePlatform>(devicePlatform, true, out var parsed))
        {
            return parsed;
        }

        return DevicePlatform.Windows;
    }

    private void TrackRegisterFailure(string reason, string? userId, string? platform, string remoteIp)
    {
        AuthTelemetry.RecordRegisterFailure(reason);
        _logger.LogWarning("PAKE register failure for {UserId} (platform={Platform}) from {RemoteIp}: {Reason}",
            AuthTelemetry.MaskIdentifier(userId),
            string.IsNullOrWhiteSpace(platform) ? "unknown" : platform,
            remoteIp,
            reason);
    }

    private void TrackRegisterSuccess(string? userId, bool hasVault, string remoteIp)
    {
        AuthTelemetry.RecordRegisterSuccess();
        _logger.LogInformation("PAKE register succeeded for {UserId} (hasVault={HasVault}) from {RemoteIp}",
            AuthTelemetry.MaskIdentifier(userId),
            hasVault,
            remoteIp);
    }

    private void TrackLoginFailure(string reason, string? userId, string? clientId, string? platform)
    {
        AuthTelemetry.RecordLoginFailure(reason);
        _logger.LogWarning("PAKE login failure for {UserId} client={ClientId} platform={Platform}: {Reason}",
            AuthTelemetry.MaskIdentifier(userId),
            string.IsNullOrWhiteSpace(clientId) ? "unknown" : clientId,
            string.IsNullOrWhiteSpace(platform) ? "unknown" : platform,
            reason);
    }

    private void TrackLoginSuccess(string? userId, Guid tenantId, string? deviceId)
    {
        AuthTelemetry.RecordLoginSuccess(tenantId);
        _logger.LogInformation("PAKE login succeeded for {UserId} tenant={TenantId} device={DeviceId}",
            AuthTelemetry.MaskIdentifier(userId),
            tenantId,
            string.IsNullOrWhiteSpace(deviceId) ? "unknown" : deviceId);
    }

    private static readonly DateTime Argon2CutoverUtc = new(2025, 11, 13, 0, 0, 0, DateTimeKind.Utc);

    private static string? NormalizeKdfParameters(PakeCredential credential)
    {
        var parsed = TryParseKdf(credential.KdfParametersJson);
        if (parsed == null)
        {
            return credential.SaltBase64 != null ? SerializeLegacyKdf(credential.SaltBase64) : null;
        }

        if (string.Equals(parsed.Algorithm, "sha256", StringComparison.OrdinalIgnoreCase))
        {
            return credential.KdfParametersJson;
        }

        if (string.Equals(parsed.Algorithm, "argon2id", StringComparison.OrdinalIgnoreCase))
        {
            // Use the original creation time to decide if this credential predates the Argon2 verifier fix.
            // UpdatedAt can change for unrelated updates and must not influence KDF selection.
            var provisionedAt = credential.CreatedAt;
            if (provisionedAt >= Argon2CutoverUtc)
            {
                return credential.KdfParametersJson;
            }

            // Accounts created before the Argon2 fix stored SHA-based verifiers even though
            // the metadata said Argon2. Force those users back to the legacy derivation.
            return credential.SaltBase64 != null ? SerializeLegacyKdf(credential.SaltBase64) : null;
        }

        return credential.SaltBase64 != null ? SerializeLegacyKdf(credential.SaltBase64) : null;
    }

    private static SrpKdfParameters? TryParseKdf(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SrpKdfParameters>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string SerializeLegacyKdf(string saltBase64)
    {
        var legacy = new SrpKdfParameters("sha256", saltBase64, degreeOfParallelism: 0, iterations: 0, memorySizeKiB: 0, aad: false);
        return JsonSerializer.Serialize(legacy);
    }

    private async Task LogAuthEventAsync(string eventType, string? userId, bool success, string? failureReason = null, string? deviceId = null, string? deviceName = null, string? metadataJson = null)
    {
        try
        {
            var entry = new AuthEventLog
            {
                EventType = eventType,
                UserId = userId,
                IsSuccess = success,
                FailureReason = failureReason,
                DeviceId = deviceId,
                DeviceName = deviceName,
                RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                MetadataJson = metadataJson
            };

            _db.AuthEventLogs.Add(entry);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to record auth event {EventType}", eventType);
        }
    }
}
