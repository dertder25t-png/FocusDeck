using Microsoft.Maui.Storage;

namespace FocusDeck.Mobile.Services.Auth;

public class MobileTokenStore
{
    private const string AccessTokenKey = "focusdeck_access_token";
    private const string RefreshTokenKey = "focusdeck_refresh_token";
    private const string UserIdKey = "focusdeck_user_id";

    public async Task SaveAsync(string userId, string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(UserIdKey, userId);
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public Task<string?> GetAccessTokenAsync() => SecureStorage.Default.GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(RefreshTokenKey);

    public Task<string?> GetUserIdAsync() => SecureStorage.Default.GetAsync(UserIdKey);

    public void Clear()
    {
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
    }
}

