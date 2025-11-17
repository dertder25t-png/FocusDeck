# Context Snapshot Implementation Notes

This document outlines the work that has been done to implement the Context Snapshot feature and what's left to do.

## What's Been Done

*   **Domain Entities:** The `ContextSnapshot`, `ContextSlice`, `ContextSourceType`, and `ContextSnapshotMetadata` entities have been created in `src/FocusDeck.Domain/Entities/Context/`.
*   **Repositories/Interfaces:** The `IContextSnapshotRepository` and `IEfContextSnapshotRepository` interfaces have been created in `src/FocusDeck.Contracts/Repositories/`, and the `EfContextSnapshotRepository` implementation has been created in `src/FocusDeck.Persistence/Repositories/Context/`.
*   **DTOs:** The `ContextSnapshotDto` has been created in `src/FocusDeck.Contracts/DTOs/`.
*   **Controllers:** The `ContextController` has been created in `src/FocusDeck.Server/Controllers/V1/Context/`.
*   **Snapshot Source Interface:** The `IContextSnapshotSource` interface has been created in `src/FocusDeck.Services/Context/`.
*   **EF Core Integration:** The `ContextSnapshot` and `ContextSlice` entities have been wired into EF Core, and the `AutomationDbContext` has been updated.
*   **`SnapshotIngestService`:** The `SnapshotIngestService` has been implemented in `src/FocusDeck.Server/Services/Context/`.
*   **`ContextSnapshotService`:** The `ContextSnapshotService` has been implemented in `src/FocusDeck.Services/Context/`.
*   **`VectorizeSnapshotJob`:** The `VectorizeSnapshotJob` has been implemented in `src/FocusDeck.Server/Jobs/`.
*   **`LayeredContextService`:** The `LayeredContextService` has been updated to consume snapshots.

## What's Left to Do

*   **Create a database migration:** A database migration needs to be created to apply the new entities to the database schema. See `docs/ContextSnapshot-Migration-Instructions.md` for instructions.
*   **Implement Snapshot Sources:** The stub implementations of the snapshot sources need to be replaced with real implementations that capture data from the corresponding APIs.
*   **Implement `IVectorStore`:** The `IVectorStore` interface needs to be implemented to store and retrieve vector embeddings.
*   **Add tests:** Unit and integration tests need to be added for the new functionality.
*   **Integrate the client:** The client applications need to be updated to send context snapshots to the new `/v1/context/snapshots` endpoint.
*   **Finish `LayeredContextService`:** The `LayeredContextService` needs to be updated to fully utilize the context snapshots for all context layers.
