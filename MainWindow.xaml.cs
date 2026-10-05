using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Seal.Services;

namespace Seal;

public partial class MainWindow : Window
{
    private readonly FocusTimer focusTimer;
    private readonly StatisticsService statistics;
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private StatisticsWindow? statisticsWindow;
    private DateTime nextCheckpoint = DateTime.UtcNow.AddSeconds(30);

    public MainWindow(FocusTimer focusTimer, StatisticsService statistics)
    {
        InitializeComponent();
        this.focusTimer = focusTimer;
        this.statistics = statistics;
        refreshTimer.Tick += Refresh;
        refreshTimer.Start();
        Closing += (_, _) =>
        {
            ExecuteSafely(focusTimer.Pause);
            refreshTimer.Stop();
            statisticsWindow?.Close();
        };
    }

    private void Refresh(object? sender, EventArgs e)
    {
        try
        {
            if (focusTimer.Update())
            {
                System.Media.SystemSounds.Asterisk.Play();
            }

            if (focusTimer.IsRunning && DateTime.UtcNow >= nextCheckpoint)
            {
                focusTimer.Checkpoint();
                nextCheckpoint = DateTime.UtcNow.AddSeconds(30);
            }
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            StatusDisplay.Text = "Unable to save history";
            return;
        }

        var seconds = (int)Math.Ceiling(focusTimer.Remaining.TotalSeconds);
        TimeDisplay.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        FocusProgress.Value = focusTimer.Elapsed.TotalSeconds / focusTimer.Duration.TotalSeconds * 100;
        ToggleIcon.Data = (System.Windows.Media.Geometry)FindResource(focusTimer.IsRunning ? "MaterialPause" : "MaterialPlay");
        var action = focusTimer.IsRunning ? "Pause" : focusTimer.Remaining == TimeSpan.Zero ? "Start" : focusTimer.HasStarted ? "Resume" : "Start";
        ToggleButton.ToolTip = action;
        System.Windows.Automation.AutomationProperties.SetName(ToggleButton, action);
        StatusDisplay.Text = focusTimer.IsRunning ? "Focus time" : focusTimer.Remaining == TimeSpan.Zero ? "Session complete" : focusTimer.HasStarted ? "Paused" : "Ready to focus";
    }

    private void ToggleTimer(object sender, RoutedEventArgs e)
    {
        ExecuteSafely(() =>
        {
            if (focusTimer.IsRunning)
            {
                focusTimer.Pause();
            }
            else
            {
                focusTimer.Start();
            }

            Refresh(null, EventArgs.Empty);
        });
    }

    private void ResetTimer(object sender, RoutedEventArgs e)
    {
        ExecuteSafely(() =>
        {
            focusTimer.Reset();
            Refresh(null, EventArgs.Empty);
        });
    }

    private void ExecuteSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show(this, "Unable to access session history. Check the file in your local Seal data folder.",
                "Seal", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenStatistics(object sender, RoutedEventArgs e) => ExecuteSafely(OpenStatisticsWindow);

    private void OpenStatisticsWindow()
    {
        focusTimer.Checkpoint();

        if (statisticsWindow is null)
        {
            statisticsWindow = new StatisticsWindow(statistics) { Owner = this };
            statisticsWindow.Closed += (_, _) => statisticsWindow = null;
            statisticsWindow.Show();
        }

        statisticsWindow.RefreshData();
        statisticsWindow.Activate();
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        WindowDrag.Handle(this, e);
    }

    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
}
