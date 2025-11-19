namespace FocusDeck.Contracts.Services.Context
{
    public interface IEmbeddingGenerationService
    {
        Task<float[]> GenerateEmbeddingAsync(string inputText);
        int Dimensions { get; }
        string ModelName { get; }
    }
}
