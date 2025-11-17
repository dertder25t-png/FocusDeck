# Context Snapshot Implementation Notes

This document outlines the work that has been done to implement the Context Snapshot feature and what's left to do.

## What's Been Done

*   **`ISnapshotIngestService` and `SnapshotIngestService`:** The `ISnapshotIngestService` interface and a placeholder implementation have been created in `src/FocusDeck.Server/Services/Context/`. The placeholder implementation currently logs incoming snapshots.
*   **`ContextSnapshotDto`:** The `ContextSnapshotDto` has been created in `src/FocusDeck.Contracts/DTOs/`.
*   **`ContextSnapshotsController`:** The `ContextSnapshotsController` has been created in `src/FocusDeck.Server/Controllers/v1/`. This controller exposes a `/v1/jarvis/snapshots` endpoint for clients to send snapshot data.
*   **Dependency Injection:** The `SnapshotIngestService` has been registered with the dependency injection container in `src/FocusDeck.Server/Startup.cs`.

## What's Left to Do

*   **Define the `ContextSnapshot` entity:** The `ContextSnapshot` entity needs to be created in `src/FocusDeck.Domain/Entities/`. The entity should include the following properties: `Id`, `UserId`, `TenantId`, `EventType`, `Timestamp`, `ActiveApplication`, `ActiveWindowTitle`, `CalendarEventId`, `CourseContext`, and `MachineState`.
*   **Update `AutomationDbContext`:** The `AutomationDbContext` needs to be updated to include a `DbSet<ContextSnapshot>` property.
*   **Create a database migration:** A database migration needs to be created to apply the new entity to the database schema.
*   **Implement `SnapshotIngestService`:** The placeholder implementation of the `SnapshotIngestService` needs to be replaced with the actual implementation that ingests snapshots into the database.
*   **Add tests:** Unit and integration tests need to be added for the new functionality.
