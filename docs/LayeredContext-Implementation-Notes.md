# Layered Context Implementation Notes

This document provides the necessary context and instructions to make the Layered Context feature fully functional.

## What's Been Done

*   **`ContextDto.cs`:** The `LayeredContextDto` has been created in `src/FocusDeck.Contracts/DTOs/`.
*   **`ILayeredContextService` and `LayeredContextService`:** The `ILayeredContextService` interface and a placeholder implementation have been created in `src/FocusDeck.Server/Services/Jarvis/`. The placeholder implementation currently returns hardcoded context layers.
*   **`IExampleGenerator` and `ExampleGenerator`:** The `IExampleGenerator` interface and a placeholder implementation have been created in `src/FocusDeck.Server/Services/Jarvis/`. The placeholder implementation currently returns hardcoded examples.
*   **Dependency Injection:** The `LayeredContextService` and `ExampleGenerator` have been registered with the dependency injection container in `src/FocusDeck.Server/Startup.cs`.

## What's Left to Do

*   **Implement `LayeredContextService`:** The `LayeredContextService` needs to be updated to fetch and assemble data from various sources to build the context layers. This will involve injecting services to access session, project, and historical data.
*   **Implement `ExampleGenerator`:** The `ExampleGenerator` needs to be updated to generate few-shot examples from historical data by querying the vector database. This will involve injecting a service for vector search.
*   **Add tests:** Unit and integration tests need to be added for the new functionality.
