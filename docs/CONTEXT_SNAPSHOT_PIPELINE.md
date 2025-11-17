# Context Snapshot Pipeline

## Purpose

The Context Snapshot Pipeline is a system designed to capture, store, and process snapshots of a user's context at a specific point in time. This information is used to power various features within FocusDeck, including Jarvis, BMAD, and the web UI.

## Flow

1.  **Capture:** The `ContextSnapshotService` is responsible for capturing a new context snapshot. It does this by calling all registered `IContextSnapshotSource` instances to get context slices.
2.  **Merge:** The service then merges the slices in order of priority. The default priority is:
    1.  Active window
    2.  Calendar
    3.  Canvas
    4.  AI ambient tags
3.  **Save:** The final JSON blob is saved to the `ContextSnapshotRepository`.
4.  **Enqueue:** A background job is enqueued for vectorization.

## Snapshot Schema

A context snapshot is a JSON object with the following properties:

*   `id`: A unique identifier for the snapshot.
*   `userId`: The ID of the user associated with the snapshot.
*   `timestamp`: The timestamp when the snapshot was taken.
*   `slices`: A collection of context slices that make up the snapshot.
*   `metadata`: Metadata associated with the snapshot.

A context slice is a JSON object with the following properties:

*   `id`: A unique identifier for the slice.
*   `sourceType`: The type of the source that generated this slice.
*   `timestamp`: The timestamp when the slice was captured.
*   `data`: The data payload of the slice as a JSON object.

## Sources

The following sources are used to capture context slices:

*   **DesktopActiveWindowSource:** Captures the active window on the user's desktop.
*   **GoogleCalendarSource:** Captures the user's upcoming Google Calendar event.
*   **CanvasAssignmentsSource:** Captures the user's upcoming Canvas assignment.
*   **SpotifySource:** Captures the user's currently playing Spotify song.
*   **DeviceActivitySource:** Captures the user's device activity.
*   **SuggestiveContextSource:** Generates a suggestive context slice using an AI model.

## Background Jobs

A background job is used to vectorize the context snapshots. This is done to enable efficient similarity searches.

## Storage

Context snapshots are stored in a PostgreSQL database. The `pgvector` extension is used for vector storage and similarity search.

## Consumers

The following consumers use the context snapshots:

*   **Jarvis:** Uses the snapshots to provide proactive assistance to the user.
*   **BMAD:** Uses the snapshots to generate personalized recommendations.
*   **Web UI:** Uses the snapshots to display the user's current context.
