namespace FocusDeck.Contracts.Services.Context
{
    public interface IEmbeddingGenerationService
    {
        Task<float[]> GenerateEmbeddingAsync(string inputText);
        Task<List<float[]>> GenerateBatchEmbeddingsAsync(IEnumerable<string> inputs);
        int Dimensions { get; }
        string ModelName { get; }
    }
}
