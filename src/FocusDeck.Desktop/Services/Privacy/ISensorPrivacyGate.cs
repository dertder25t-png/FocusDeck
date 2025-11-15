using System.Threading;

namespace FocusDeck.Desktop.Services.Privacy;

internal interface ISensorPrivacyGate
{
    Task<bool> IsEnabledAsync(string contextType, CancellationToken cancellationToken = default);
}
