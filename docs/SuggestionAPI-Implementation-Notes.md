# Suggestion API Implementation Notes

This document provides the necessary context and instructions to make the Suggestion API feature fully functional.

## What's Been Done

*   **`SuggestionDto.cs`:** The `SuggestionRequestDto` and `SuggestionResponseDto` have been created in `src/FocusDeck.Contracts/DTOs/`.
*   **`ISuggestionService` and `SuggestionService`:** The `ISuggestionService` interface and a placeholder implementation have been created in `src/FocusDeck.Server/Services/Jarvis/`. The placeholder implementation currently contains a simple rule-based MVP.
*   **`JarvisController`:** The `JarvisController` has been updated to include a `/v1/jarvis/suggest` endpoint that uses the `ISuggestionService`.
*   **Dependency Injection:** The `SuggestionService` has been registered with the dependency injection container in `src/FocusDeck.Server/Startup.cs`.

## What's Left to Do

*   **Implement the rule-based MVP:** The `SuggestionService` currently contains a simple example of a rule-based MVP. This needs to be expanded to include more rules and logic.
*   **Upgrade to a vector-driven approach:** The `SuggestionService` should be upgraded to use a vector-driven approach. This will involve the following steps:
    1.  Generate an embedding for the `request.CurrentContext`.
    2.  Perform a similarity search against the `ContextVectors` table in the database.
    3.  Retrieve the top N most similar historical snapshots.
    4.  Use the retrieved snapshots to formulate a more informed suggestion.
*   **Add tests:** Unit and integration tests need to be added for the new functionality.
