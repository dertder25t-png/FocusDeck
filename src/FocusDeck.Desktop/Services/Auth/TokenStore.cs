using System.Text.Json;
using System.IO;

namespace FocusDeck.Desktop.Services.Auth;

public class TokenStore
{
    private readonly string _path;

    public class TokenRecord
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? UserId { get; set; }
    }

    public TokenStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "FocusDeck", "Auth");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "tokens.json");
    }

    public void Save(string accessToken, string? refreshToken, string? userId)
    {
        var rec = new TokenRecord { AccessToken = accessToken, RefreshToken = refreshToken, UserId = userId };
        var json = JsonSerializer.Serialize(rec, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    public TokenRecord? Load()
    {
        try
        {
            if (!File.Exists(_path)) return null;
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<TokenRecord>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }
}
