using System.Collections.Generic;

namespace FocusDeck.Server.Controllers.Models
{
    /// <summary>
    /// Defines the UI and instructions needed to connect a service.
    /// </summary>
    public sealed class ServiceSetupGuide
    {
        /// <summary>
        /// "Simple" (token/URL) or "OAuth" (button click).
        /// </summary>
        public string SetupType { get; set; } = string.Empty;

        /// <summary>
        /// e.g., "Connect Home Assistant"
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Main description of the setup process.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Step-by-step instructions for setting up the service.
        /// </summary>
        public IReadOnlyList<string>? Steps { get; set; }

        /// <summary>
        /// Helpful documentation links.
        /// </summary>
        public IReadOnlyList<SetupLink>? Links { get; set; }

        /// <summary>
        /// Required server-side configuration (appsettings.json).
        /// </summary>
        public IReadOnlyList<string>? RequiredServerConfig { get; set; }

        /// <summary>
        /// A list of fields the user must fill out (for "Simple" setup).
        /// </summary>
        public IReadOnlyList<SetupField>? Fields { get; set; }

        /// <summary>
        /// The text for the connect button (for "OAuth" setup).
        /// </summary>
        public string? OAuthButtonText { get; set; }
    }

    /// <summary>
    /// Represents a documentation link.
    /// </summary>
    public sealed class SetupLink
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a single field in a "Simple" setup form.
    /// </summary>
    public sealed class SetupField
    {
        /// <summary>
        /// The key to use when sending data to the 'connect' endpoint (e.g., "haBaseUrl", "access_token").
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The user-friendly label for the input (e.g., "Home Assistant URL").
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Instructions on how to find this value.
        /// </summary>
        public string HelpText { get; set; } = string.Empty;

        /// <summary>
        /// "text" or "password".
        /// </summary>
        public string InputType { get; set; } = "text";
    }
}
