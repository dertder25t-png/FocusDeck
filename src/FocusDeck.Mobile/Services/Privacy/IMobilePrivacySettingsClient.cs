using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Mobile.Services.Privacy;

internal interface IMobilePrivacySettingsClient
{
    Task<IReadOnlyList<PrivacySettingDto>> GetConsentAsync(CancellationToken cancellationToken = default);
    Task<PrivacySettingDto?> UpdateConsentAsync(string contextType, bool isEnabled, CancellationToken cancellationToken = default);
}
