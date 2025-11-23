namespace FocusDeck.Mobile.Services;

/// <summary>
/// Defines the capabilities (skills) supported by the Android agent.
/// These strings match the 'Kind' field in RemoteAction protocol.
/// </summary>
public static class AndroidCapabilities
{
    public const string ShowToast = "ShowToast";
    public const string OpenUrl = "OpenUrl";
    public const string OpenNote = "OpenNote";
    public const string OpenDeck = "OpenDeck"; // Future: Open a specific flashcard deck
    public const string StartFocus = "StartFocus"; // Future: Trigger focus mode

    /// <summary>
    /// Returns a list of all supported skills for advertisement to the server.
    /// </summary>
    public static readonly string[] All =
    {
        ShowToast,
        OpenUrl,
        OpenNote,
        OpenDeck,
        StartFocus
    };
}
