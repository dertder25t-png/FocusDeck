using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Server.Services.Burnout;

public interface IBurnoutAnalysisService
{
    Task AnalyzePatternsAsync(CancellationToken cancellationToken = default);
}
