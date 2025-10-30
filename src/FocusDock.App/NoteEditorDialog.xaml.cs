using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FocusDock.Core.Services;
using FocusDock.Data.Models;
using FocusDock.App.Controls;

namespace FocusDock.App;

public partial class NoteEditorDialog : Window
{
    private const double SidebarWidth = 280;
    private const int MaxBookmarkHighlightLength = 240;

    private static readonly string[] BookmarkColorPalette =
    {
        "#FFD700", "#FF69B4", "#00CED1", "#FF6347", "#32CD32", "#FF8C00", "#9370DB"
    };

    private static readonly Random BookmarkColorRandom = new();

    private readonly NotesService _notesService;
    private readonly Note _note;
    private readonly bool _isEdit;

    private bool _bookmarksSidebarVisible;
    private bool _isFocusMode;
    private bool _hasBeenPersisted;
    private string _lastSavedTitle = string.Empty;
    private string _lastSavedContent = string.Empty;
    private string _lastSavedTagsKey = string.Empty;
    private DispatcherTimer? _statusTimer;

    public NoteEditorDialog(NotesService notesService, Note? existingNote)
    {
        _notesService = notesService;
        _note = existingNote ?? new Note();
        _isEdit = existingNote != null;
        _bookmarksSidebarVisible = false;
    _isFocusMode = false;
        _hasBeenPersisted = _isEdit;
        
        InitializeComponent();

        _note.Bookmarks ??= new List<NoteBookmark>();
        _note.Tags ??= new List<string>();

        if (_isEdit && BtnSave != null)
        {
            this.Title = "Edit Note";
            BtnSave.Content = "Update Note";
        }
        
        LoadNoteData();
        UpdateSavedSnapshot(_note.Title, _note.Content, BuildTagsKey(_note.Tags));
        
        // Keyboard shortcuts
        if (TxtContent != null)
        {
            TxtContent.PreviewKeyDown += OnContentKeyDown;
            TxtContent.PreviewKeyDown += OnContentPreviewKeyDown;
            
            // Character counter
            TxtContent.TextChanged += (s, e) =>
            {
                if (TxtCharCount != null)
                {
                    TxtCharCount.Text = $"{TxtContent.Text.Length} characters";
                }
            };
        }

        // Global shortcuts (e.g., F11)
        this.PreviewKeyDown += OnWindowPreviewKeyDown;
    }

    private void LoadNoteData()
    {
        if (TxtTitle != null)
        {
            TxtTitle.Text = _note.Title;
        }

        if (TxtContent != null)
        {
            TxtContent.Text = _note.Content;
            if (TxtCharCount != null)
            {
                TxtCharCount.Text = $"{TxtContent.Text.Length} characters";
            }
        }

        if (_note.Tags.Any() && TxtTags != null)
        {
            TxtTags.Text = string.Join(", ", _note.Tags);
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        // Save changes (if any) and close back to list
        AutoSaveNote(showStatus: false);
        DialogResult = true;
        Close();
    }

    private void OnToggleFocusMode(object sender, RoutedEventArgs e)
    {
        _isFocusMode = !_isFocusMode;
        ApplyFocusMode(_isFocusMode);
    }

    private void OnWindowPreviewKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            _isFocusMode = !_isFocusMode;
            ApplyFocusMode(_isFocusMode);
            e.Handled = true;
        }
    }

    private void ApplyFocusMode(bool enable)
    {
        // Collapse header and toolbar; hide bookmarks panel; maximize editor surface
        if (HeaderGrid != null)
        {
            HeaderGrid.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
        }
        if (ToolbarBorder != null)
        {
            ToolbarBorder.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
        }

        if (BookmarksSidebar != null)
        {
            // Hide overlay and reset state when entering focus mode
            BookmarksSidebar.Width = enable ? 0 : (_bookmarksSidebarVisible ? SidebarWidth : 0);
        }
        if (EditorSurface != null)
        {
            EditorSurface.Margin = enable ? new Thickness(0, 0, 0, 0)
                                          : new Thickness(0, 18, _bookmarksSidebarVisible ? SidebarWidth + 16 : 0, 18);
        }

        if (enable)
        {
            _bookmarksSidebarVisible = false;
            // Hide transient status chip while focused
            if (StatusChip != null)
            {
                StatusChip.Visibility = Visibility.Collapsed;
                StatusChip.Opacity = 0;
            }
        }
    }

    // ===== Toolbar Formatting Handlers =====
    
    private void OnBoldClick(object sender, RoutedEventArgs e) => WrapSelection("**");
    private void OnItalicClick(object sender, RoutedEventArgs e) => WrapSelection("*");
    private void OnUnderlineClick(object sender, RoutedEventArgs e) => WrapSelection("<u>", "</u>");
    
    private void OnBulletListClick(object sender, RoutedEventArgs e) => InsertAtLineStart("- ");
    private void OnNumberedListClick(object sender, RoutedEventArgs e) => InsertAtLineStart("1. ");
    private void OnTaskListClick(object sender, RoutedEventArgs e) => InsertAtLineStart("- [ ] ");
    
    private void OnHeading1Click(object sender, RoutedEventArgs e) => InsertAtLineStart("# ");
    private void OnHeading2Click(object sender, RoutedEventArgs e) => InsertAtLineStart("## ");
    private void OnHeading3Click(object sender, RoutedEventArgs e) => InsertAtLineStart("### ");
    
    private void OnHorizontalLineClick(object sender, RoutedEventArgs e)
    {
        if (TxtContent == null) return;
        int pos = TxtContent.CaretIndex;
        TxtContent.Text = TxtContent.Text.Insert(pos, "\n---\n");
        TxtContent.CaretIndex = pos + 5;
        TxtContent.Focus();
    }
    
    private void OnQuoteClick(object sender, RoutedEventArgs e) => InsertAtLineStart("> ");
    
    private void OnCodeBlockClick(object sender, RoutedEventArgs e)
    {
        if (TxtContent == null) return;
        if (!string.IsNullOrEmpty(TxtContent.SelectedText))
            WrapSelection("```\n", "\n```");
        else
            WrapSelection("`");
    }

    // ===== Bookmark Management =====
    
    private void OnAddBookmarkClick(object sender, RoutedEventArgs e)
    {
        if (TxtContent == null) return;
        
        AutoSaveNote(showStatus: false);

        if (!_hasBeenPersisted)
        {
            ShowStatus("Save your note before adding bookmarks.", isError: true);
            return;
        }

        var text = TxtContent.Text;
        var caretPosition = Math.Clamp(TxtContent.CaretIndex, 0, text.Length);
        var selectionStart = TxtContent.SelectionLength > 0 ? TxtContent.SelectionStart : caretPosition;
        var selectionLength = TxtContent.SelectionLength > 0 ? TxtContent.SelectionLength : 0;

        var (rangeStart, rangeLength) = CalculateBookmarkRange(caretPosition, text, selectionStart, selectionLength);

        var dialog = new InputDialog("Bookmark Name", "Give this spot a quick label:")
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            var name = (dialog.ResultText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowStatus("Bookmark name cannot be empty.", isError: true);
                return;
            }

            var colorHex = BookmarkColorPalette.Length == 0
                ? "#FFD700"
                : BookmarkColorPalette[BookmarkColorRandom.Next(BookmarkColorPalette.Length)];

            var bookmark = new NoteBookmark
            {
                Name = name,
                Position = rangeStart,
                Length = rangeLength,
                Color = colorHex,
                CreatedDate = DateTime.Now
            };

            _note.Bookmarks.Add(bookmark);
            _notesService.UpdateNote(_note);
            LoadBookmarks();
            JumpToBookmark(bookmark);
            ShowStatus($"Added bookmark \"{bookmark.Name}\"");
        }
    }
    
    private bool AutoSaveNote(bool showStatus = false)
    {
        if (TxtTitle == null || TxtContent == null) return false;

        var title = TxtTitle.Text.Trim();
        var content = TxtContent.Text;
        var tags = TxtTags != null
            ? TxtTags.Text.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList()
            : new List<string>();

        var tagsKey = BuildTagsKey(tags);

        var hasChanges = !_hasBeenPersisted ||
                         !string.Equals(title, _lastSavedTitle, StringComparison.Ordinal) ||
                         !string.Equals(content, _lastSavedContent, StringComparison.Ordinal) ||
                         !string.Equals(tagsKey, _lastSavedTagsKey, StringComparison.Ordinal);

        _note.Title = title;
        _note.Content = content;
        _note.Tags = tags;

        if (!_hasBeenPersisted)
        {
            if (string.IsNullOrWhiteSpace(_note.Id))
            {
                _note.Id = Guid.NewGuid().ToString();
            }
            if (_note.CreatedDate == default)
            {
                _note.CreatedDate = DateTime.Now;
            }

            _notesService.AddNote(_note);
            _hasBeenPersisted = true;
            UpdateSavedSnapshot(title, content, tagsKey);
            if (showStatus)
            {
                ShowStatus("Note saved");
            }
            return true;
        }

        if (hasChanges)
        {
            _notesService.UpdateNote(_note);
            UpdateSavedSnapshot(title, content, tagsKey);
            if (showStatus)
            {
                ShowStatus("Changes saved");
            }
            return true;
        }

        return false;
    }
    
    private void OnShowBookmarksClick(object sender, RoutedEventArgs e)
    {
        if (BookmarksSidebar == null) return;

        AutoSaveNote(showStatus: false);

        if (!_hasBeenPersisted)
        {
            ShowStatus("Save your note before opening bookmarks.", isError: true);
            return;
        }
        
        _bookmarksSidebarVisible = !_bookmarksSidebarVisible;
        
        if (_bookmarksSidebarVisible)
        {
            BookmarksSidebar.Width = SidebarWidth;
            if (EditorSurface != null)
            {
                EditorSurface.Margin = new Thickness(0, 18, SidebarWidth + 16, 18);
            }
            LoadBookmarks();
        }
        else
        {
            BookmarksSidebar.Width = 0;
            if (EditorSurface != null)
            {
                EditorSurface.Margin = new Thickness(0, 18, 0, 18);
            }
        }
    }
    
    private void LoadBookmarks()
    {
        if (BookmarksList == null) return;
        
        BookmarksList.Children.Clear();
        
        if (_note.Bookmarks == null || !_note.Bookmarks.Any())
        {
            var emptyText = new TextBlock
            {
                Text = "No bookmarks yet",
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 178, 190)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            BookmarksList.Children.Add(emptyText);
            return;
        }
        
        foreach (var bookmark in _note.Bookmarks.OrderBy(b => b.Position))
        {
            var button = new System.Windows.Controls.Button
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 37, 40)),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 234, 237)),
                BorderThickness = new Thickness(2, 0, 0, 0),
                Padding = new Thickness(18, 12, 18, 12),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = bookmark
            };

            var preview = GetBookmarkPreview(bookmark);

            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bookmark.Color);
                button.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)36, color.R, color.G, color.B));
                button.BorderBrush = new SolidColorBrush(color);
            }
            catch
            {
                button.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(123, 95, 255));
            }

            var nameText = new TextBlock
            {
                Text = bookmark.Name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 234, 237))
            };

            var contentStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };
            contentStack.Children.Add(nameText);

            if (!string.IsNullOrWhiteSpace(preview))
            {
                contentStack.Children.Add(new TextBlock
                {
                    Text = preview,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 178, 190)),
                    Margin = new Thickness(0, 6, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = SidebarWidth - 60
                });

                button.ToolTip = new TextBlock
                {
                    Text = preview,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = SidebarWidth + 40,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 234, 237))
                };
            }

            button.Content = contentStack;

            button.Click += (s, e) => JumpToBookmark(bookmark);

            var contextMenu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Delete Bookmark" };
            deleteItem.Click += (s, e) => DeleteBookmark(bookmark);
            contextMenu.Items.Add(deleteItem);
            button.ContextMenu = contextMenu;

            BookmarksList.Children.Add(button);
        }
    }
    
    private void JumpToBookmark(NoteBookmark bookmark)
    {
        if (TxtContent == null) return;
        
        var text = TxtContent.Text;
        if (string.IsNullOrEmpty(text))
        {
            ShowStatus("Bookmark no longer matches this note.", isError: true);
            return;
        }

        var (start, length) = NormalizeBookmarkRange(bookmark.Position, bookmark.Length, text.Length);

        if (start >= text.Length)
        {
            ShowStatus("Bookmark is outside the current text.", isError: true);
            return;
        }

        TxtContent.SelectionStart = start;
        TxtContent.SelectionLength = length;
        TxtContent.Focus();

        var lineIndex = TxtContent.GetLineIndexFromCharacterIndex(Math.Min(start, Math.Max(0, text.Length - 1)));
        TxtContent.ScrollToLine(Math.Max(0, lineIndex - 2));

        ShowStatus($"Jumped to \"{bookmark.Name}\"");
    }
    
    private void DeleteBookmark(NoteBookmark bookmark)
    {
        var result = System.Windows.MessageBox.Show(
            $"Delete bookmark '{bookmark.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _note.Bookmarks.Remove(bookmark);
            _notesService.UpdateNote(_note);
            LoadBookmarks();
            ShowStatus($"Deleted bookmark \"{bookmark.Name}\"");
        }
    }

    // ===== Formatting Helper Methods =====
    
    private void WrapSelection(string wrapper)
    {
        if (TxtContent == null || string.IsNullOrEmpty(TxtContent.SelectedText)) return;
        
        int start = TxtContent.SelectionStart;
        int length = TxtContent.SelectionLength;
        string selected = TxtContent.SelectedText;
        string wrapped = wrapper + selected + wrapper;
        
        TxtContent.Text = TxtContent.Text.Remove(start, length).Insert(start, wrapped);
        TxtContent.SelectionStart = start + wrapper.Length;
        TxtContent.SelectionLength = selected.Length;
        TxtContent.Focus();
    }
    
    private void WrapSelection(string startWrap, string endWrap)
    {
        if (TxtContent == null || string.IsNullOrEmpty(TxtContent.SelectedText)) return;
        
        int start = TxtContent.SelectionStart;
        int length = TxtContent.SelectionLength;
        string selected = TxtContent.SelectedText;
        string wrapped = startWrap + selected + endWrap;
        
        TxtContent.Text = TxtContent.Text.Remove(start, length).Insert(start, wrapped);
        TxtContent.SelectionStart = start + startWrap.Length;
        TxtContent.SelectionLength = selected.Length;
        TxtContent.Focus();
    }
    
    private void InsertAtLineStart(string prefix)
    {
        if (TxtContent == null) return;
        
        int caretPos = TxtContent.CaretIndex;
        string text = TxtContent.Text;
        
        // Find start of current line
        int lineStart = text.LastIndexOf('\n', caretPos > 0 ? caretPos - 1 : 0) + 1;
        
        TxtContent.Text = text.Insert(lineStart, prefix);
        TxtContent.CaretIndex = caretPos + prefix.Length;
        TxtContent.Focus();
    }
    
    // ===== Keyboard Shortcuts =====
    
    private void OnContentKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.B:
                    OnBoldClick(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.I:
                    OnItalicClick(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.U:
                    OnUnderlineClick(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }
    }

    private void OnContentPreviewKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if (TxtContent == null) return;

        if (e.Key == Key.Tab)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                var text = TxtContent.Text;
                var caret = TxtContent.SelectionStart;
                var lineStart = text.LastIndexOf('\n', caret > 0 ? caret - 1 : 0) + 1;
                int spacesToRemove = 0;
                while (spacesToRemove < 4 && lineStart + spacesToRemove < text.Length && text[lineStart + spacesToRemove] == ' ')
                {
                    spacesToRemove++;
                }

                if (spacesToRemove > 0)
                {
                    TxtContent.Text = text.Remove(lineStart, spacesToRemove);
                    var newCaret = Math.Max(lineStart, caret - spacesToRemove);
                    TxtContent.SelectionStart = newCaret;
                    TxtContent.SelectionLength = 0;
                }
            }
            else
            {
                var index = TxtContent.SelectionStart;
                TxtContent.Text = TxtContent.Text.Insert(index, "    ");
                TxtContent.SelectionStart = index + 4;
                TxtContent.SelectionLength = 0;
            }
            e.Handled = true;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        AutoSaveNote(showStatus: false);

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private (int start, int length) CalculateBookmarkRange(int caretIndex, string text, int selectionStart, int selectionLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (0, 0);
        }

        if (selectionLength > 0)
        {
            return NormalizeBookmarkRange(selectionStart, selectionLength, text.Length);
        }

        int start = caretIndex;
        int end = caretIndex;

        while (start > 0 && !IsBookmarkBoundary(text[start - 1]))
        {
            start--;
        }

        while (end < text.Length && !IsBookmarkBoundary(text[end]))
        {
            end++;
        }

        if (end < text.Length && ".?!".Contains(text[end]))
        {
            end++;
        }

        if (start == end)
        {
            start = Math.Max(0, caretIndex - 80);
            end = Math.Min(text.Length, caretIndex + 80);
        }

        return NormalizeBookmarkRange(start, end - start, text.Length);
    }

    private static bool IsBookmarkBoundary(char c)
    {
        return c == '\n' || c == '\r' || c == '\t' || c == '.' || c == '!' || c == '?' || c == ';' || c == ':';
    }

    private (int start, int length) NormalizeBookmarkRange(int start, int length, int textLength)
    {
        if (textLength <= 0)
        {
            return (0, 0);
        }

        start = Math.Max(0, Math.Min(start, textLength - 1));
        var available = textLength - start;

        if (available < 0)
        {
            available = 0;
        }

        if (length <= 0 || length > available)
        {
            length = available;
        }

        length = Math.Min(length, MaxBookmarkHighlightLength);

        return (start, Math.Max(0, length));
    }

    private string GetBookmarkPreview(NoteBookmark bookmark)
    {
        var text = TxtContent?.Text ?? _note.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var (start, length) = NormalizeBookmarkRange(bookmark.Position, bookmark.Length, text.Length);
        if (length <= 0)
        {
            return string.Empty;
        }

        length = Math.Min(length, Math.Min(160, text.Length - start));
        if (length <= 0)
        {
            return string.Empty;
        }

        var snippet = text.Substring(start, length).Replace('\r', ' ').Replace('\n', ' ');
        snippet = Regex.Replace(snippet, @"\s+", " ").Trim();
        return snippet;
    }

    private string BuildTagsKey(IEnumerable<string> tags) => string.Join("|", tags);

    private void UpdateSavedSnapshot(string title, string content, string tagsKey)
    {
        _lastSavedTitle = title;
        _lastSavedContent = content;
        _lastSavedTagsKey = tagsKey;
    }

    private void ShowStatus(string message, bool isError = false)
    {
        if (StatusChip == null || StatusMessage == null) return;

        if (TryFindResource(isError ? "StatusErrorBrush" : "StatusInfoBrush") is System.Windows.Media.Brush background)
        {
            StatusChip.Background = background;
        }

        StatusMessage.Text = message;
        StatusChip.Visibility = Visibility.Visible;

        var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        StatusChip.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        _statusTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusTimer.Stop();
        _statusTimer.Tick -= OnStatusTimerTick;
        _statusTimer.Tick += OnStatusTimerTick;
        _statusTimer.Start();
    }

    private void OnStatusTimerTick(object? sender, EventArgs e)
    {
        _statusTimer?.Stop();

        if (StatusChip == null) return;

        var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            if (StatusChip != null)
            {
                StatusChip.Visibility = Visibility.Collapsed;
            }
        };
        StatusChip.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}
