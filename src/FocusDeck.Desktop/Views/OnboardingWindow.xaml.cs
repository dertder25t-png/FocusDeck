using System;
using System.Threading.Tasks;
using System.Windows;
using FocusDeck.Desktop.Services;
using FocusDeck.Desktop.Services.Auth;

namespace FocusDeck.Desktop.Views;

public partial class OnboardingWindow : Window
{
    private readonly IKeyProvisioningService _provisioning;
    private readonly IApiClient _api;

    public OnboardingWindow()
    {
        InitializeComponent();
        _provisioning = (IKeyProvisioningService)App.Current.Services.GetService(typeof(IKeyProvisioningService))!;
        _api = (IApiClient)App.Current.Services.GetService(typeof(IApiClient))!;
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        ClearMessages();
        var user = UserIdBox.Text.Trim();
        var pass = PasswordBox.Password;
        try
        {
            var ok = await _provisioning.RegisterAsync(user, pass);
            if (ok)
            {
                StatusText.Text = "Registered successfully. You can now Login.";
            }
            else ErrorText.Text = "Registration failed.";
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        ClearMessages();
        var user = UserIdBox.Text.Trim();
        var pass = PasswordBox.Password;
        try
        {
            var tokens = await _provisioning.LoginAsync(user, pass);
            if (tokens.HasValue)
            {
                _api.AccessToken = tokens.Value.accessToken;
                StatusText.Text = "Signed in.";
                DialogResult = true;
                Close();
            }
            else ErrorText.Text = "Login failed.";
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private async void StartPair_Click(object sender, RoutedEventArgs e)
    {
        ClearMessages();
        try
        {
            var (pairingId, code) = await _provisioning.PairStartAsync();
            PairCodeText.Text = $"Code: {code}";
            PairingIdText.Text = pairingId.ToString();
            StatusText.Text = "Pairing started.";
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private async void Transfer_Click(object sender, RoutedEventArgs e)
    {
        ClearMessages();
        try
        {
            if (!Guid.TryParse(PairingIdText.Text, out var pairingId))
            {
                ErrorText.Text = "No valid pairing ID. Click 'Start Pairing' first.";
                return;
            }
            var password = TransferPasswordBox.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Enter your password to encrypt the vault.";
                return;
            }
            var ok = await _provisioning.PairTransferAsync(pairingId, password);
            if (ok) StatusText.Text = "Vault uploaded. Redeem on the other device."; else ErrorText.Text = "Upload failed.";
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Redeem_Click(object sender, RoutedEventArgs e)
    {
        ClearMessages();
        try
        {
            if (!Guid.TryParse(RedeemPairingIdBox.Text.Trim(), out var pairingId))
            {
                ErrorText.Text = "Invalid pairing ID";
                return;
            }
            var code = RedeemCodeBox.Text.Trim();
            var password = RedeemPasswordBox.Password;
            var ok = await _provisioning.PairRedeemAsync(pairingId, code, password);
            if (ok)
            {
                StatusText.Text = "Vault imported. You can now login.";
            }
            else
            {
                ErrorText.Text = "Redeem failed.";
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void ClearMessages()
    {
        StatusText.Text = string.Empty;
        ErrorText.Text = string.Empty;
    }
}
