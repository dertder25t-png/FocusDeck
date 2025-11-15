using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Auth;

namespace FocusDeck.Server.Tests;

internal sealed class InMemoryCryptographicKeyStore : ICryptographicKeyStore
{
    private string _primary;
    private string? _secondary;

    public InMemoryCryptographicKeyStore(string primary, string? secondary = null)
    {
        _primary = primary;
        _secondary = secondary;
    }

    public Task<string> GetPrimaryKeyAsync(CancellationToken ct = default) => Task.FromResult(_primary);

    public Task<string?> GetSecondaryKeyAsync(CancellationToken ct = default) => Task.FromResult(_secondary);

    public Task RotateKeyAsync(string newPrimaryKey, CancellationToken ct = default)
    {
        _secondary = newPrimaryKey;
        return Task.CompletedTask;
    }

    public void SetPrimary(string key) => _primary = key;

    public void SetSecondary(string? key) => _secondary = key;
}
