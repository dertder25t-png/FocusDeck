using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Mobile.Services.Privacy;

internal interface IMobileCloudApiClient
{
    Task<HttpResponseMessage?> SendAsync(HttpMethod method, string path, object? content = null, CancellationToken cancellationToken = default);
}
