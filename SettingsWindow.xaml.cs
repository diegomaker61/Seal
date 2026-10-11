using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Seal.Models;

namespace Seal;

public partial class SettingsWindow : Window
{
    private AppSettings draft;
    private bool isCapturingShortcut;
    private readonly Action<AppSettings> applySettings;

    public SettingsWindow(AppSettings settings, Action<AppSettings> applySettings)
    {
        InitializeComponent();
        draft = settings;
        this.applySettings = applySettings;
        ShortcutPreview.Text = draft.ShortcutLabel;
        BorderOption.IsChecked = draft.ShowBorder;
        StartupOption.IsChecked = draft.StartWithWindows;
        DurationInput.ItemsSource = Enumerable.Range(1, 6).Select(value => value * 10);
        DurationInput.SelectedItem = Math.Clamp((int)Math.Round(draft.FocusMinutes / 10.0, MidpointRounding.AwayFromZero) * 10, 10, 60);
    }

    private void BeginShortcutCapture(object sender, RoutedEventArgs e)
    {
        isCapturingShortcut = true;
        UpdateShortcutPreview();
        ShortcutButton.Content = "Press your shortcut…";
        ShortcutButton.Background = (System.Windows.Media.Brush)FindResource("AccentBrush");
        ShortcutHint.Text = "Listening… Press Ctrl/Alt/Windows + a key. Esc to cancel.";
        FeedbackLabel.Text = "";
        ShortcutButton.Focus();
    }

    private void StopShortcutCapture(object sender, KeyboardFocusChangedEventArgs e) => EndShortcutCapture();

    private void EndShortcutCapture()
    {
        isCapturingShortcut = false;
        ShortcutPreview.Text = draft.ShortcutLabel;
        ShortcutButton.Content = "Change shortcut";
        ShortcutButton.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
        ShortcutHint.Text = "Click Change shortcut, then press your combination.";
    }

    private void CaptureShortcut(object sender, KeyEventArgs e)
    {
        if (!isCapturingShortcut)
        {
            return;
        }

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            EndShortcutCapture();
            return;
        }

        UpdateShortcutPreview(key);

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var candidate = draft with { ShortcutKey = key, ShortcutModifiers = Keyboard.Modifiers };
        try
        {
            candidate.Validate();
            draft = candidate;
            ShortcutPreview.Text = draft.ShortcutLabel;
            EndShortcutCapture();
            ShortcutHint.Text = "Shortcut captured. Click Save to apply.";
        }
        catch (ArgumentException)
        {
            ShortcutHint.Text = "Use Ctrl, Alt or Windows together with a key. Esc to cancel.";
        }
    }

    private void PreviewShortcutRelease(object sender, KeyEventArgs e)
    {
        if (isCapturingShortcut)
        {
            e.Handled = true;
            UpdateShortcutPreview();
        }
    }

    private void UpdateShortcutPreview(Key key = Key.None)
    {
        var modifiers = Keyboard.Modifiers;
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Windows");

        if (key is not (Key.None or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin))
        {
            parts.Add(key is >= Key.D0 and <= Key.D9 ? ((int)key - (int)Key.D0).ToString() : key.ToString());
        }

        ShortcutPreview.Text = parts.Count == 0 ? "Waiting for keys…" : string.Join(" + ", parts);
    }

    private void SaveSettings(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DurationInput.SelectedItem is not int minutes)
            {
                throw new ArgumentException("Enter a duration between 10 and 60 minutes.");
            }

            var settings = draft with
            {
                FocusMinutes = minutes,
                ShowBorder = BorderOption.IsChecked == true,
                StartWithWindows = StartupOption.IsChecked == true
            };
            settings.Validate();
            applySettings(settings);
            Close();
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or
            UnauthorizedAccessException or JsonException or System.Security.SecurityException)
        {
            FeedbackLabel.Text = error.Message;
        }
    }

    private void DragWindow(object sender, MouseButtonEventArgs e) => WindowDrag.Handle(this, e);
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
}
