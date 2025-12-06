using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// Defines the contract for a service that builds a layered context for the user.
/// </summary>
public interface ILayeredContextService
{
    /// <summary>
    /// Builds a layered context for the user.
    /// </summary>
    /// <param name="userId">The ID of the user to build context for.</param>
    /// <returns>A DTO containing the different layers of context.</returns>
    Task<LayeredContextDto> BuildContextAsync(Guid userId);
}
