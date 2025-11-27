using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Server.Services.Auth;

public interface ICryptographicKeyStore
{
    Task<string> GetPrimaryKeyAsync(CancellationToken ct = default);
    Task<string?> GetSecondaryKeyAsync(CancellationToken ct = default);
    Task RotateKeyAsync(string newPrimaryKey, CancellationToken ct = default);
}
