using System.Collections.Generic;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Server.Services.Jarvis
{
    public interface IAutomationGeneratorService
    {
        /// <summary>
        /// Generates an automation proposal based on a cluster of recurring context snapshots.
        /// </summary>
        /// <param name="cluster">A list of similar context snapshots representing a pattern.</param>
        /// <returns>Task representing the async operation.</returns>
        Task GenerateProposalAsync(List<ContextSnapshot> cluster);
    }
}
