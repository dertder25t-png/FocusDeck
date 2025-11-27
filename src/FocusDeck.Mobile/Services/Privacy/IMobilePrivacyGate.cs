using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Mobile.Services.Privacy;

internal interface IMobilePrivacyGate
{
    Task<bool> IsEnabledAsync(string contextType, CancellationToken cancellationToken = default);
}
