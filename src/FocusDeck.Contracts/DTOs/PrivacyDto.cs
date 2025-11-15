using FocusDeck.SharedKernel.Privacy;

namespace FocusDeck.Contracts.DTOs;

public sealed record PrivacySettingDto(
    string ContextType,
    string DisplayName,
    string Description,
    bool IsEnabled,
    PrivacyTier Tier,
    bool DefaultEnabled);

public sealed record PrivacySettingUpdateDto(
    string ContextType,
    bool IsEnabled,
    PrivacyTier? Tier = null);
