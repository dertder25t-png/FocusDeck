using System.Collections.Generic;
using System.Linq;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel.Privacy;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Privacy;

internal sealed record PrivacyContextDefinition(
    string ContextType,
    string DisplayName,
    string Description,
    PrivacyTier Tier,
    bool DefaultEnabled);

public sealed class PrivacyService : IPrivacyService
{
    private static readonly IReadOnlyList<PrivacyContextDefinition> s_contextDefinitions = new[]
    {
        new PrivacyContextDefinition(
            "ActiveWindowTitle",
            "Active Window Title",
            "The current window or tab title so Jarvis can understand which app you are using.",
            PrivacyTier.Medium,
            false),

        new PrivacyContextDefinition(
            "TypingVelocity",
            "Typing Velocity",
            "Keystrokes per minute to help detect bursts and pauses in typing.",
            PrivacyTier.Medium,
            false),

        new PrivacyContextDefinition(
            "MouseEntropy",
            "Mouse Entropy",
            "Mouse movement intensity and randomness to spot flow state.",
            PrivacyTier.Medium,
            false),

        new PrivacyContextDefinition(
            "AmbientNoise",
            "Ambient Noise",
            "Microphone noise level to approximate focus vs. distraction.",
            PrivacyTier.High,
            false),

        new PrivacyContextDefinition(
            "DeviceMotion",
            "Device Motion",
            "Device acceleration/orientation for mobile context.",
            PrivacyTier.Medium,
            false),

        new PrivacyContextDefinition(
            "ScreenState",
            "Screen State",
            "Whether the screen is on/off or locked.",
            PrivacyTier.Medium,
            false),

        new PrivacyContextDefinition(
            "PhysicalLocation",
            "Physical Location",
            "Optional coarse location hints (if enabled) for context-aware rules.",
            PrivacyTier.High,
            false),
    };

    private static readonly IReadOnlyDictionary<string, PrivacyContextDefinition> s_definitionLookup =
        s_contextDefinitions.ToDictionary(x => x.ContextType, StringComparer.OrdinalIgnoreCase);

    private readonly AutomationDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<PrivacyService> _logger;

    public PrivacyService(
        AutomationDbContext db,
        ICurrentTenant currentTenant,
        ILogger<PrivacyService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PrivacySettingDto>> GetSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var stored = await _db.PrivacySettings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId == userId)
            .ToDictionaryAsync(
                x => x.ContextType,
                x => x,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        var list = s_contextDefinitions
            .Select(def =>
            {
                stored.TryGetValue(def.ContextType, out var record);
                var isEnabled = record?.IsEnabled ?? def.DefaultEnabled;
                var tier = record?.Tier ?? def.Tier;
                return new PrivacySettingDto(def.ContextType, def.DisplayName, def.Description, isEnabled, tier, def.DefaultEnabled);
            })
            .ToList();

        return list;
    }

    public async Task<PrivacySettingDto> UpdateSettingAsync(string userId, PrivacySettingUpdateDto dto, CancellationToken cancellationToken)
    {
        var def = TryGetDefinition(dto.ContextType);
        if (def == null)
        {
            throw new ArgumentException($"Unknown context type '{dto.ContextType}'", nameof(dto.ContextType));
        }

        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var normalized = def.ContextType;

        var existing = await _db.PrivacySettings
            .SingleOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.ContextType.Equals(normalized, StringComparison.OrdinalIgnoreCase),
                cancellationToken);

        if (existing == null)
        {
            existing = new PrivacySetting
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                ContextType = normalized,
                IsEnabled = dto.IsEnabled,
                Tier = dto.Tier ?? def.Tier,
                UpdatedAt = DateTime.UtcNow
            };
            _db.PrivacySettings.Add(existing);
        }
        else
        {
            existing.IsEnabled = dto.IsEnabled;
            if (dto.Tier.HasValue)
            {
                existing.Tier = dto.Tier.Value;
            }
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PrivacySettingDto(def.ContextType, def.DisplayName, def.Description, existing.IsEnabled, existing.Tier, def.DefaultEnabled);
    }

    public async Task<bool> IsEnabledAsync(string userId, string contextType, CancellationToken cancellationToken)
    {
        var def = TryGetDefinition(contextType);
        if (def == null)
        {
            _logger.LogWarning("Privacy check requested for unknown context type '{ContextType}'", contextType);
            return false;
        }

        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var record = await _db.PrivacySettings
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.ContextType.Equals(def.ContextType, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefaultAsync(cancellationToken);

        return record?.IsEnabled ?? def.DefaultEnabled;
    }

    private static PrivacyContextDefinition? TryGetDefinition(string? contextType)
    {
        if (string.IsNullOrWhiteSpace(contextType))
        {
            return null;
        }

        s_definitionLookup.TryGetValue(contextType.Trim(), out var def);
        return def;
    }
}
