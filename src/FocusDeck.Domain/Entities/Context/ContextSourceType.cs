namespace FocusDeck.Domain.Entities.Context
{
    /// <summary>
    /// Defines the different sources that can contribute to a context snapshot.
    /// </summary>
    public enum ContextSourceType
    {
        /// <summary>
        /// The active window on the user's desktop.
        /// </summary>
        DesktopActiveWindow,

        /// <summary>
        /// The user's Google Calendar.
        /// </summary>
        GoogleCalendar,

        /// <summary>
        /// The user's Canvas assignments.
        /// </summary>
        CanvasAssignments,

        /// <summary>
        /// The user's Spotify activity.
        /// </summary>
        Spotify,

        /// <summary>
        /// The user's device activity.
        /// </summary>
        DeviceActivity,

        /// <summary>
        /// A suggestive context source.
        /// </summary>
        SuggestiveContext,

        /// <summary>
        /// Represents a state change in the system (e.g. Focus Session started).
        /// </summary>
        SystemStateChange,

        /// <summary>
        /// Mobile application usage activity.
        /// </summary>
        MobileAppUsage
    }
}
