# Jarvis Runs Implementation Notes

This document outlines the work that has been done to implement the Jarvis Runs feature and what's left to do.

## What's Been Done

*   **Entities:** The `JarvisRun` and `JarvisRunStep` entities have been created, along with the `JarvisRunStatus` and `JarvisRunStepType` enums.
*   **Repo:** The `IJarvisRunRepository` interface and a placeholder `EfJarvisRunRepository` implementation have been created.
*   **Run Service:** The `IJarvisRunService` interface and a placeholder `JarvisRunService` implementation have been created.
*   **Job Skeleton:** The `IJarvisRunJob` interface and a placeholder `JarvisRunJob` implementation have been created.
*   **Controllers:** The `JarvisRunsController` has been created with placeholder endpoints.

## What's Left to Do

*   **Implement `EfJarvisRunRepository`:** The placeholder implementation of the `EfJarvisRunRepository` needs to be replaced with the actual implementation that interacts with the database.
*   **Implement `JarvisRunService`:** The placeholder implementation of the `JarvisRunService` needs to be replaced with the actual implementation that orchestrates Jarvis runs.
*   **Implement `JarvisRunJob`:** The placeholder implementation of the `JarvisRunJob` needs to be replaced with the actual implementation that executes Jarvis runs.
*   **Implement `JarvisRunsController`:** The placeholder implementation of the `JarvisRunsController` needs to be replaced with the actual implementation that handles API requests.
*   **Real LLM Integration:** The system needs to be integrated with a real Large Language Model.
*   **Real Actions:** The placeholder `NoOpActionHandler` needs to be replaced with real action handlers.
*   **Error Handling:** Robust error handling needs to be implemented throughout the system.
*   **Add tests:** Unit and integration tests need to be added for the new functionality.
