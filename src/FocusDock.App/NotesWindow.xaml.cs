using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FocusDock.Core.Services;
using FocusDock.Data.Models;

namespace FocusDock.App;

public partial class NotesWindow : Window
{
    private readonly NotesService _notesService;
    private string _searchQuery = "";
    private bool _isGridView = true;

    public NotesWindow(NotesService notesService)
    {
        _notesService = notesService;
        InitializeComponent();
        
        _notesService.NotesChanged += (s, e) =>
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() => RefreshNotes()));
            }
            catch { /* Window might be closing */ }
        };
        
        Loaded += (s, e) => RefreshNotes();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtSearch == null) return;
        _searchQuery = TxtSearch.Text.ToLower();
        RefreshNotes();
    }

    private void OnGridViewClick(object sender, RoutedEventArgs e)
    {
        _isGridView = true;
        RefreshNotes();
    }

    private void OnListViewClick(object sender, RoutedEventArgs e)
    {
        _isGridView = false;
        RefreshNotes();
    }

    private void OnNewNoteClick(object sender, RoutedEventArgs e)
    {
        var editor = new NoteEditorDialog(_notesService, null) { Owner = this };
        editor.ShowDialog();
    }

    private void RefreshNotes()
    {
        if (NotesContainer == null) return;
        
        NotesContainer.Children.Clear();

        var notes = _notesService.GetAllNotes()
            .Where(n => string.IsNullOrWhiteSpace(_searchQuery) ||
                       n.Title.ToLower().Contains(_searchQuery) ||
                       n.Content.ToLower().Contains(_searchQuery) ||
                       n.Tags.Any(t => t.ToLower().Contains(_searchQuery)))
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.LastModified ?? n.CreatedDate)
            .ToList();

        if (!notes.Any())
        {
            var emptyMsg = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(_searchQuery) 
                    ? "No notes yet. Click '+ New Note' to create your first note!" 
                    : "No notes found matching your search.",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 178, 190)),
                FontSize = 16,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 60, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            NotesContainer.Children.Add(emptyMsg);
            return;
        }

        foreach (var note in notes)
        {
            var noteCard = CreateNoteCard(note);
            NotesContainer.Children.Add(noteCard);
        }
    }

    private Border CreateNoteCard(Note note)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(29, 30, 32)), // Lighter than background
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255)), // Subtle white border
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16), // Softer, more modern corners
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 16, 16),
            Width = _isGridView ? 300 : double.NaN,
            Height = _isGridView ? 250 : double.NaN,
            Cursor = System.Windows.Input.Cursors.Hand,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Color.FromRgb(0, 0, 0),
                Direction = 270,
                ShadowDepth = 2,
                BlurRadius = 16,
                Opacity = 0.3
            }
        };

        // Hover animation effect
        card.MouseEnter += (s, e) =>
        {
            card.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 124, 92, 255)); // Purple glow
            var scaleTransform = new System.Windows.Media.ScaleTransform(1.02, 1.02);
            card.RenderTransform = scaleTransform;
            card.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        };

        card.MouseLeave += (s, e) =>
        {
            card.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255));
            card.RenderTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Date
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tags
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

        // Date in top-right corner
        var dateHeader = new TextBlock
        {
            Text = (note.LastModified ?? note.CreatedDate).ToString("MMM d, yyyy"),
            FontSize = 11,
            FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 12)
        };
        grid.Children.Add(dateHeader);
        Grid.SetRow(dateHeader, 0);

        // Content
        var contentStack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        
        // Title with pin indicator
        var titlePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        
        if (note.IsPinned)
        {
            var pinIcon = new TextBlock
            {
                Text = "📌",
                FontSize = 14,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(pinIcon);
        }

        var titleText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(note.Title) ? "Untitled" : note.Title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(242, 255, 255, 255)), // 95% white opacity
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 50
        };
        titlePanel.Children.Add(titleText);
        contentStack.Children.Add(titlePanel);

        // Content preview
        if (!string.IsNullOrWhiteSpace(note.Content))
        {
            var contentText = new TextBlock
            {
                Text = note.Content,
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 178, 190)),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = _isGridView ? 100 : 60
            };
            contentStack.Children.Add(contentText);
        }

        grid.Children.Add(contentStack);
        Grid.SetRow(contentStack, 1);

        // Tags
        if (note.Tags.Any())
        {
            var tagsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            foreach (var tag in note.Tags.Take(3))
            {
                var tagChip = new Border
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 124, 92, 255)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 6, 0)
                };
                var tagText = new TextBlock
                {
                    Text = tag,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(124, 92, 255))
                };
                tagChip.Child = tagText;
                tagsPanel.Children.Add(tagChip);
            }
            grid.Children.Add(tagsPanel);
            Grid.SetRow(tagsPanel, 2);
        }

        // Footer with actions
        var footer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Action buttons (no date here anymore, it's in the top-right)
        var actionsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        
        var pinBtn = new System.Windows.Controls.Button
        {
            Content = note.IsPinned ? "📌" : "📍",
            FontSize = 16,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = note.IsPinned ? "Unpin" : "Pin",
            Margin = new Thickness(0, 0, 8, 0)
        };
        pinBtn.Click += (s, e) =>
        {
            e.Handled = true;
            _notesService.TogglePin(note.Id);
        };
        actionsPanel.Children.Add(pinBtn);

        var editBtn = new System.Windows.Controls.Button
        {
            Content = "✏️",
            FontSize = 16,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Edit",
            Margin = new Thickness(0, 0, 8, 0)
        };
        editBtn.Click += (s, e) =>
        {
            e.Handled = true;
            var editor = new NoteEditorDialog(_notesService, note) { Owner = this };
            editor.ShowDialog();
        };
        actionsPanel.Children.Add(editBtn);

        var deleteBtn = new System.Windows.Controls.Button
        {
            Content = "🗑️",
            FontSize = 16,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Delete"
        };
        deleteBtn.Click += (s, e) =>
        {
            e.Handled = true;
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete this note?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                _notesService.DeleteNote(note.Id);
            }
        };
        actionsPanel.Children.Add(deleteBtn);

        footer.Children.Add(actionsPanel);
        Grid.SetColumn(actionsPanel, 1);

        grid.Children.Add(footer);
        Grid.SetRow(footer, 3);

        card.Child = grid;

        // Click to edit
        card.MouseLeftButtonDown += (s, e) =>
        {
            var editor = new NoteEditorDialog(_notesService, note) { Owner = this };
            editor.ShowDialog();
        };

        return card;
    }
}
