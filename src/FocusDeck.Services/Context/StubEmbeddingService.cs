using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Context;

namespace FocusDeck.Services.Context
{
    public class StubEmbeddingService : IEmbeddingGenerationService
    {
        public int Dimensions => 384; // Simulating all-MiniLM-L6-v2
        public string ModelName => "stub-random-projection";

        public Task<float[]> GenerateEmbeddingAsync(string inputText)
        {
            // Deterministic stub: hash input to seed a random generator
            // This ensures the same text gets the same vector (useful for testing)
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputText));
            var seed = BitConverter.ToInt32(hashBytes, 0);
            
            var random = new Random(seed);
            var vector = new float[Dimensions];
            
            for (int i = 0; i < Dimensions; i++)
            {
                // Generate random float between -1 and 1
                vector[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            // Normalize vector to unit length (cosine similarity ready)
            var norm = (float)Math.Sqrt(vector.Sum(x => x * x));
            if (norm > 0)
            {
                for (int i = 0; i < Dimensions; i++)
                {
                    vector[i] /= norm;
                }
            }

            return Task.FromResult(vector);
        }
    }
}
