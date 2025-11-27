# Vectorization Implementation Notes

This document provides the necessary context and instructions to make the vectorization feature fully functional.

## 1. Set Up the Vector Database

This implementation is designed to work with PostgreSQL and the `pgvector` extension.

### Enable the `pgvector` Extension

Connect to your PostgreSQL database and run the following command:

```sql
CREATE EXTENSION vector;
```

## 2. Create the `ContextVectors` Table

Run the following SQL command to create the table that will store the vector embeddings. The `vector(384)` column is sized for the `all-MiniLM-L6-v2` model, but you can adjust it to match the dimensions of your chosen embedding model.

```sql
CREATE TABLE "ContextVectors" (
    "Id" UUID PRIMARY KEY,
    "SnapshotId" UUID NOT NULL,
    "Vector" vector(384),
    "CreatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "FK_ContextVectors_ContextSnapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "ContextSnapshots" ("Id") ON DELETE CASCADE
);
```

## 3. Implement an Embedding Generation Service

The `VectorizeSnapshotJob` is designed to use an `IEmbeddingGenerationService` to create the vector embeddings. You will need to create this service and its interface.

### Example Interface

```csharp
// src/FocusDeck.Server/Services/TextGeneration/IEmbeddingGenerationService.cs
public interface IEmbeddingGenerationService
{
    Task<float[]> GenerateEmbeddingAsync(string inputText);
}
```

### Example Implementation

You can use a library like `Microsoft.ML.OnnxRuntime` to run a local embedding model or connect to a third-party API like OpenAI.

## 4. Update the `VectorizeSnapshotJob`

Once you have an `IEmbeddingGenerationService`, you can uncomment the commented-out code in the `VectorizeSnapshotJob` and inject your new service.

```csharp
// src/FocusDeck.Server/Jobs/VectorizeSnapshotJob.cs

// ...

// 1. Uncomment the private field
// private readonly IEmbeddingGenerationService _embeddingService;

// 2. Uncomment the constructor parameter
// public VectorizeSnapshotJob(..., IEmbeddingGenerationService embeddingService)

// 3. Uncomment the service assignment
// _embeddingService = embeddingService;

// 4. Uncomment the implementation in the Execute method
// ...
```
