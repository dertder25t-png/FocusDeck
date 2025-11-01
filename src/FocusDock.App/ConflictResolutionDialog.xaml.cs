using System.Text.Json;
using System.Windows;
using FocusDeck.Shared.Models.Sync;

namespace FocusDock.App;

public partial class ConflictResolutionDialog : Window
{
    private readonly SyncConflict _conflict;
    private ConflictResolution _choice = ConflictResolution.Manual;

    public ConflictResolutionDialog(SyncConflict conflict)
    {
        InitializeComponent();
        _conflict = conflict;
        Title = $"Resolve Conflict • {_conflict.EntityType} {_conflict.EntityId}";

        // Pretty print JSON
        try
        {
            var localDoc = JsonDocument.Parse(_conflict.LocalChange.DataJson);
            TxtLocal.Text = JsonSerializer.Serialize(localDoc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { TxtLocal.Text = _conflict.LocalChange.DataJson; }

        try
        {
            var serverDoc = JsonDocument.Parse(_conflict.ServerChange.DataJson);
            TxtServer.Text = JsonSerializer.Serialize(serverDoc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { TxtServer.Text = _conflict.ServerChange.DataJson; }
    }

    public ConflictResolution GetChoice() => _choice;

    private void OnUseServer(object sender, RoutedEventArgs e)
    {
        _choice = ConflictResolution.UseServer;
        DialogResult = true;
        Close();
    }

    private void OnUseLocal(object sender, RoutedEventArgs e)
    {
        _choice = ConflictResolution.UseLocal;
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _choice = ConflictResolution.Manual;
        DialogResult = false;
        Close();
    }
}
