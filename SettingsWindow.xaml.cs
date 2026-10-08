using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Seal.Models;

namespace Seal;

public partial class SettingsWindow : Window
{
    private AppSettings draft;
    private readonly Action<AppSettings> applySettings;

    public SettingsWindow(AppSettings settings, Action<AppSettings> applySettings)
    {
        InitializeComponent();
        draft = settings;
        this.applySettings = applySettings;
        ShortcutInput.Text = draft.ShortcutLabel;
        BorderOption.IsChecked = draft.ShowBorder;
        StartupOption.IsChecked = draft.StartWithWindows;
        DurationInput.ItemsSource = Enumerable.Range(1, 6).Select(value => value * 10);
        DurationInput.SelectedItem = Math.Clamp((int)Math.Round(draft.FocusMinutes / 10.0, MidpointRounding.AwayFromZero) * 10, 10, 60);
    }

    private void CaptureShortcut(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            return;
        }

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var candidate = draft with { ShortcutKey = key, ShortcutModifiers = Keyboard.Modifiers };

        try
        {
            candidate.Validate();
            draft = candidate;
            ShortcutInput.Text = draft.ShortcutLabel;
            FeedbackLabel.Text = "";
        }
        catch (ArgumentException)
        {
            FeedbackLabel.Text = "Use Ctrl, Alt or Windows together with a key.";
        }
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
