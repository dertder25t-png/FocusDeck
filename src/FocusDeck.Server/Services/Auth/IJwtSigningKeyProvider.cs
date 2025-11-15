using System.Collections.Generic;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace FocusDeck.Server.Services.Auth;

public interface IJwtSigningKeyProvider
{
    IEnumerable<SecurityKey> GetValidationKeys();
    Task<string> GetPrimaryKeyAsync(CancellationToken ct = default);
    bool ContainsVersion(string version);
    void InvalidateCache();
}
