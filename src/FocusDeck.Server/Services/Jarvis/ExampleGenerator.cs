using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// A service that generates few-shot examples from historical data by querying a vector database.
/// </summary>
public class ExampleGenerator : IExampleGenerator
{
    private readonly ILogger<ExampleGenerator> _logger;
    // In a real implementation, you would inject a service for vector search.
    // private readonly IVectorSearchService _vectorSearchService;

    public ExampleGenerator(ILogger<ExampleGenerator> logger /*, IVectorSearchService vectorSearchService */)
    {
        _logger = logger;
        // _vectorSearchService = vectorSearchService;
    }

    /// <summary>
    /// Generates few-shot examples by finding similar historical contexts in the vector database.
    /// </summary>
    /// <param name="context">The current user context, used as the query for the similarity search.</param>
    /// <returns>A list of formatted strings representing the few-shot examples.</returns>
    public Task<List<string>> GenerateExamplesAsync(string context)
    {
        _logger.LogInformation("Generating few-shot examples for context: {Context}", context);

        // STEP 1: Generate an embedding for the input context.
        // This would use the same embedding service as the vectorization job.
        // var queryVector = await _embeddingService.GenerateEmbeddingAsync(context);

        // STEP 2: Perform a similarity search in the vector database.
        // The IVectorSearchService would be responsible for this, using the queryVector
        // to find the top N most similar historical snapshots.
        // var similarSnapshots = await _vectorSearchService.FindSimilarSnapshotsAsync(queryVector, topN: 3);

        // STEP 3: Format the retrieved snapshots into few-shot examples.
        // Each example should be a string that demonstrates a "context -> action" pair.
        /*
        var examples = new List<string>();
        foreach (var snapshot in similarSnapshots)
        {
            // This assumes you can retrieve the action that was taken following the snapshot.
            var subsequentAction = GetActionForSnapshot(snapshot.Id);
            examples.Add($"Context: {snapshot.Summary}, Action: {subsequentAction}");
        }
        return examples;
        */

        // Placeholder implementation.
        var placeholderExamples = new List<string>
        {
            "Context: User is in a meeting about 'Project X', Action: start_note 'Meeting Notes'",
            "Context: User is browsing 'github.com', Action: open_project 'WebApp.csproj'"
        };

        return Task.FromResult(placeholderExamples);
    }
}
