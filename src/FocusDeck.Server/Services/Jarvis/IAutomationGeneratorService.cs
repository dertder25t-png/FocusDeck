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

        /// <summary>
        /// Generates an automation proposal based on a user intent (prompt).
        /// </summary>
        /// <param name="intent">The user's description of what they want to automate.</param>
        /// <returns>Task representing the async operation.</returns>
        Task<FocusDeck.Domain.Entities.Automations.AutomationProposal> GenerateProposalFromIntentAsync(string intent, string userId);
    }
}
