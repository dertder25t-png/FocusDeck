using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Server.Services.Privacy;

public interface IPrivacyService
{
    Task<IReadOnlyList<PrivacySettingDto>> GetSettingsAsync(string userId, CancellationToken cancellationToken);
    Task<PrivacySettingDto> UpdateSettingAsync(string userId, PrivacySettingUpdateDto dto, CancellationToken cancellationToken);
    Task<bool> IsEnabledAsync(string userId, string contextType, CancellationToken cancellationToken);
}
