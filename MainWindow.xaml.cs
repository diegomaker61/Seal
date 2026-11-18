using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Seal.Services;
using Seal.Models;

namespace Seal;

public partial class MainWindow : Window
{
    private readonly FocusTimer focusTimer;
    private readonly TaskSelection taskSelection = new();
    private readonly MediaPauseService mediaPause = new();
    private AppSettings settings;
    private readonly SettingsRepository settingsRepository;
    private readonly WindowsStartupService windowsStartup = new();
    private SettingsWindow? settingsWindow;
    private readonly GlobalTimerShortcut timerShortcut;
    private readonly FocusBorderController focusBorder = new();
    private readonly TaskCatalog tasks;
    private TasksWindow? tasksWindow;
    private readonly StatisticsService statistics;
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private StatisticsWindow? statisticsWindow;
    private DateTime roundsDay;
    private DateTime nextCheckpoint = DateTime.UtcNow.AddSeconds(30);

    public MainWindow(FocusTimer focusTimer, StatisticsService statistics, TaskCatalog tasks, AppSettings? settings = null, SettingsRepository? settingsRepository = null)
    {
        InitializeComponent();
        AppNameLabel.Text = AppDataPaths.IsDevelopment ? "Seal (Dev)" : "Seal";
        Title = AppNameLabel.Text;
        this.focusTimer = focusTimer;
        this.statistics = statistics;
        this.tasks = tasks;
        this.settingsRepository = settingsRepository ?? new SettingsRepository();
        this.settings = settings ?? this.settingsRepository.Load();
        timerShortcut = new GlobalTimerShortcut(this, () => ToggleTimer(this, new RoutedEventArgs()), this.settings);
        tasks.Changed += TasksChanged;
        RefreshTaskPicker();
        TaskPicker.SelectionChanged += (_, _) => { if (TaskPicker.SelectedItem is FocusTask task) taskSelection.Select(task.Id); };
        taskSelection.Changed += (_, _) => TaskPicker.SelectedValue = taskSelection.SelectedId;
        if (TaskPicker.SelectedItem is FocusTask initialTask) taskSelection.Select(initialTask.Id);
        Refresh(null, EventArgs.Empty);
        refreshTimer.Tick += Refresh;
        refreshTimer.Start();
        Closing += (_, _) =>
        {
            ExecuteSafely(focusTimer.Pause);
            refreshTimer.Stop();
            timerShortcut.Dispose();
            focusBorder.Dispose();
            statisticsWindow?.Close();
            tasksWindow?.Close();
            settingsWindow?.Close();
            tasks.Changed -= TasksChanged;
        };
    }

    private void Refresh(object? sender, EventArgs e)
    {
        try
        {
            if (roundsDay != DateTime.Today) RefreshDailyRounds();
            if (focusTimer.Update())
            {
                System.Media.SystemSounds.Asterisk.Play();
                RefreshTaskPicker();
            }

            if (focusTimer.IsRunning && DateTime.UtcNow >= nextCheckpoint)
            {
                focusTimer.Checkpoint();
                nextCheckpoint = DateTime.UtcNow.AddSeconds(30);
            }
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            focusBorder.UpdateVisibility(focusTimer.IsRunning && settings.ShowBorder);
            StatusDisplay.Text = "Unable to save history";
            return;
        }

        focusBorder.UpdateVisibility(focusTimer.IsRunning && settings.ShowBorder);
        taskSelection.IsLocked = focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero;
        TaskPicker.IsEnabled = !taskSelection.IsLocked;
        if (statisticsWindow is not null) statisticsWindow.SetSelectionLocked(taskSelection.IsLocked);
        CompleteButton.IsEnabled = focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero;
        var seconds = (int)Math.Ceiling(focusTimer.Remaining.TotalSeconds);
        TimeDisplay.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        FocusProgress.Value = focusTimer.Elapsed.TotalSeconds / focusTimer.Duration.TotalSeconds * 100;
        ToggleIcon.Data = (System.Windows.Media.Geometry)FindResource(focusTimer.IsRunning ? "MaterialPause" : "MaterialPlay");
        var action = focusTimer.IsRunning ? "Pause" : focusTimer.Remaining == TimeSpan.Zero ? "Start" : focusTimer.HasStarted ? "Resume" : "Start";
        ToggleButton.ToolTip = timerShortcut.IsRegistered
            ? $"{action} ({settings.ShortcutLabel})"
            : $"{action} — shortcut unavailable ({settings.ShortcutLabel} is in use)";
        System.Windows.Automation.AutomationProperties.SetName(ToggleButton, action);
        StatusDisplay.Text = focusTimer.IsRunning ? "Focus time" : focusTimer.Remaining == TimeSpan.Zero ? "Session complete" : focusTimer.HasStarted ? "Paused" : "Ready to focus";
    }

    private async void ToggleTimer(object sender, RoutedEventArgs e)
    {
        var pauseMedia = focusTimer.IsRunning && settings.PauseMediaOnPause;
        var resumeMedia = !focusTimer.IsRunning && focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero
            && settings.PauseMediaOnPause;
        ExecuteSafely(() =>
        {
            if (focusTimer.IsRunning)
            {
                focusTimer.Pause();
            }
            else
            {
                focusTimer.Start(TaskPicker.SelectedItem as FocusTask);
            }

            Refresh(null, EventArgs.Empty);
        });
        if (pauseMedia && !await mediaPause.PausePlayingAsync())
        {
            MessageBox.Show(this, "Some media could not be paused. Check whether the player supports Windows media controls.",
                "Seal", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        if (resumeMedia && !await mediaPause.ResumePausedAsync())
        {
            MessageBox.Show(this, "Some media could not be resumed. The player may have been closed or stopped.",
                "Seal", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ResetTimer(object sender, RoutedEventArgs e)
    {
        ExecuteSafely(() =>
        {
            focusTimer.Reset();
            mediaPause.ForgetPausedSessions();
            RefreshTaskPicker();
            Refresh(null, EventArgs.Empty);
        });
    }

    private void CompleteTimer(object sender, RoutedEventArgs e) => ExecuteSafely(() =>
    {
        focusTimer.Complete();
        RefreshDailyRounds();
        mediaPause.ForgetPausedSessions();
        Refresh(null, EventArgs.Empty);
        statisticsWindow?.RefreshData();
    });

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

    private void OpenMainMenu(object sender, RoutedEventArgs e)
    {
        var menu = MenuButton.ContextMenu;
        menu.PlacementTarget = MenuButton;
        menu.IsOpen = !menu.IsOpen;
    }

    private void OpenStatistics(object sender, RoutedEventArgs e) => ExecuteSafely(OpenStatisticsWindow);

    private void OpenStatisticsWindow()
    {
        focusTimer.Checkpoint();

        if (statisticsWindow is null)
        {
            statisticsWindow = new StatisticsWindow(statistics, tasks, taskSelection) { Owner = this };
            statisticsWindow.Closed += (_, _) => statisticsWindow = null;
            statisticsWindow.Show();
        }

        statisticsWindow.RefreshData();
        statisticsWindow.Activate();
    }

    private void TasksChanged(object? sender, EventArgs e) => RefreshTaskPicker();

    private void RefreshTaskPicker()
    {
        var selected = focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero
            ? focusTimer.CurrentTask
            : TaskPicker.SelectedItem as FocusTask;
        var available = tasks.ActiveTasks().ToList();

        if (selected is not null && focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero &&
            !available.Any(task => task.Id == selected.Id))
        {
            available.Add(selected);
        }

        RefreshDailyRounds();
        TaskPicker.ItemsSource = available;
        TaskPicker.SelectedItem = available.FirstOrDefault(task => task.Id == selected?.Id)
            ?? available.FirstOrDefault(task => task.Id == Guid.Empty)
            ?? available.First();
        if (TaskPicker.SelectedItem is FocusTask current)
        {
            taskSelection.IsLocked = false;
            taskSelection.Select(current.Id);
        }
    }

    private void RefreshDailyRounds()
    {
        var day = DateTime.Today;
        var completed = statistics.ForDay(day).Where(session => session.Completed)
            .GroupBy(session => session.TaskId)
            .ToDictionary(group => group.Key, group => group.Count());
        TaskPicker.Tag = new DailyRounds(settings.FocusMinutes, completed);
        roundsDay = day;
    }

    private void OpenTasks(object sender, RoutedEventArgs e) => ExecuteSafely(() =>
    {
        if (tasksWindow is null)
        {
            tasksWindow = new TasksWindow(tasks) { Owner = this };
            tasksWindow.Closed += (_, _) => tasksWindow = null;
            tasksWindow.Show();
        }

        tasksWindow.Activate();
    });

    private void OpenSettings(object sender, RoutedEventArgs e) => ExecuteSafely(() =>
    {
        if (settingsWindow is null)
        {
            settingsWindow = new SettingsWindow(settings, ApplySettings) { Owner = this };
            settingsWindow.Closed += (_, _) => settingsWindow = null;
            settingsWindow.Show();
        }

        settingsWindow.Activate();
    });

    private void ApplySettings(AppSettings updated)
    {
        updated.Validate();
        var previous = settings;

        if (!timerShortcut.TryChange(updated))
        {
            throw new InvalidOperationException("This shortcut is already in use. Choose another combination.");
        }

        try
        {
            if (updated.StartWithWindows != previous.StartWithWindows)
            {
                windowsStartup.SetEnabled(updated.StartWithWindows);
            }

            settingsRepository.Save(updated);
        }
        catch
        {
            timerShortcut.TryChange(previous);
            if (updated.StartWithWindows != previous.StartWithWindows)
            {
                windowsStartup.SetEnabled(previous.StartWithWindows);
            }

            throw;
        }

        if (!updated.PauseMediaOnPause) mediaPause.ForgetPausedSessions();
        settings = updated;
        focusTimer.ConfigureDuration(TimeSpan.FromMinutes(updated.FocusMinutes));
        RefreshDailyRounds();
        Refresh(null, EventArgs.Empty);
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        WindowDrag.Handle(this, e);
    }

    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
}
