using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Mobile.Services.Privacy;

internal interface IMobileActivitySignalClient
{
    Task SendActivitySignalAsync(ActivitySignalDto signal, CancellationToken cancellationToken = default);
}
