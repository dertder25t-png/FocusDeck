# Feedback Loop Implementation Notes

This document provides the necessary context and instructions to make the Feedback Loop feature fully functional.

## 1. Create the `FeedbackSignals` Table

Run the following SQL command to create the table that will store the feedback signals.

```sql
CREATE TABLE "FeedbackSignals" (
    "Id" UUID PRIMARY KEY,
    "SnapshotId" UUID NOT NULL,
    "Reward" DOUBLE PRECISION NOT NULL,
    "Timestamp" TIMESTAMP NOT NULL,
    CONSTRAINT "FK_FeedbackSignals_ContextSnapshots_SnapshotId" FOREIGN KEY ("SnapshotId") REFERENCES "ContextSnapshots" ("Id") ON DELETE CASCADE
);
```

## 2. Update the `FeedbackService`

Once the `FeedbackSignals` table is in place, you can uncomment the commented-out code in the `FeedbackService` to save feedback signals to the database.

```csharp
// src/FocusDeck.Server/Services/Jarvis/FeedbackService.cs

// ...

// 1. Uncomment the private field
// private readonly AutomationDbContext _dbContext;

// 2. Uncomment the constructor parameter
// public FeedbackService(..., AutomationDbContext dbContext)

// 3. Uncomment the service assignment
// _dbContext = dbContext;

// 4. Uncomment the implementation in the ProcessFeedbackAsync method
// ...
```

## 3. Implement the `ImplicitFeedbackMonitor`

The `ImplicitFeedbackMonitor` is a hosted service that runs in the background to infer implicit feedback from user actions. You will need to implement the logic to query for recent user actions, infer a reward signal, and process the feedback.

```csharp
// src/FocusDeck.Server/Services/Jarvis/ImplicitFeedbackMonitor.cs

// ...

// 1. Inject the IFeedbackService
// private readonly IFeedbackService _feedbackService;

// 2. Uncomment the constructor parameter
// public ImplicitFeedbackMonitor(..., IFeedbackService feedbackService)

// 3. Uncomment the service assignment
// _feedbackService = feedbackService;

// 4. Implement the logic in the DoWork method
// ...
```
