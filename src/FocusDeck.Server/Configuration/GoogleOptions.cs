namespace FocusDeck.Server.Configuration
{
    public class GoogleOptions
    {
        public const string SectionName = "Google";

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }
}
