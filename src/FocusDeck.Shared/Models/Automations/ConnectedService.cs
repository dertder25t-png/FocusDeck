namespace FocusDeck.Shared.Models.Automations
{
    public class ConnectedService
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!; // Link to a user if you have auth
        public ServiceType Service { get; set; }
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
