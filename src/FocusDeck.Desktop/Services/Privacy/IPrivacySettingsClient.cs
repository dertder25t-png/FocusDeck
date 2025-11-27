using System.Collections.Generic;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Desktop.Services.Privacy;

internal interface IPrivacySettingsClient
{
    Task<IReadOnlyList<PrivacySettingDto>> GetConsentAsync(CancellationToken cancellationToken = default);
    Task<PrivacySettingDto?> UpdateConsentAsync(string contextType, bool isEnabled, CancellationToken cancellationToken = default);
}
