using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private bool? cleanFocusMode;
    private bool refreshingTaskPicker;
    private bool switchingTask;
    private DateTime roundsDay;
    private DateTime nextCheckpoint = DateTime.UtcNow.AddSeconds(30);

    public MainWindow(FocusTimer focusTimer, StatisticsService statistics, TaskCatalog tasks, AppSettings? settings = null, SettingsRepository? settingsRepository = null)
    {
        InitializeComponent();
        Title = AppDataPaths.IsDevelopment ? "Seal (Dev)" : "Seal";
        this.focusTimer = focusTimer;
        this.statistics = statistics;
        this.tasks = tasks;
        this.settingsRepository = settingsRepository ?? new SettingsRepository();
        this.settings = settings ?? this.settingsRepository.Load();
        timerShortcut = new GlobalTimerShortcut(this, () => ToggleTimer(this, new RoutedEventArgs()), this.settings);
        tasks.Changed += TasksChanged;
        RefreshTaskPicker();
        TaskPicker.SelectionChanged += (_, _) => { if (!refreshingTaskPicker && TaskPicker.SelectedItem is FocusTask task) taskSelection.Select(task.Id); RefreshRoundMarkers(); };
        taskSelection.Changed += SelectedTaskChanged;
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
            TimeDisplay.ToolTip = "Unable to save history";
            return;
        }

        TimeDisplay.ToolTip = null;
        focusBorder.UpdateVisibility(focusTimer.IsRunning && settings.ShowBorder);
        taskSelection.IsLocked = false;
        TaskPicker.IsEnabled = !taskSelection.IsLocked;
        if (statisticsWindow is not null) statisticsWindow.SetSelectionLocked(taskSelection.IsLocked);
        CompleteButton.IsEnabled = focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero;
        RefreshFocusAppearance();
        var seconds = (int)Math.Ceiling(focusTimer.Remaining.TotalSeconds);
        TimeDisplay.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        FocusProgress.Value = focusTimer.Elapsed.TotalSeconds / focusTimer.Duration.TotalSeconds * 100;
        ToggleIcon.Data = (System.Windows.Media.Geometry)FindResource(focusTimer.IsRunning ? "MaterialPause" : "MaterialPlay");
        var action = focusTimer.IsRunning ? "Pause" : focusTimer.Remaining == TimeSpan.Zero ? "Start" : focusTimer.HasStarted ? "Resume" : "Start";
        ToggleButton.ToolTip = timerShortcut.IsRegistered
            ? $"{action} ({settings.ShortcutLabel})"
            : $"{action} — shortcut unavailable ({settings.ShortcutLabel} is in use)";
        System.Windows.Automation.AutomationProperties.SetName(ToggleButton, action);
    }

    private void FocusPointerChanged(object sender, MouseEventArgs e) => RefreshFocusAppearance();

    private void RefreshFocusAppearance()
    {
        if (focusTimer is null) return;
        var clean = focusTimer.IsRunning && !IsMouseOver && MenuButton.ContextMenu?.IsOpen != true;
        FocusProgress.IsShimmerActive = focusTimer.IsRunning;
        if (cleanFocusMode == clean) return;
        cleanFocusMode = clean;
        var duration = TimeSpan.FromMilliseconds(300);
        var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };
        foreach (var control in new FrameworkElement[] { TimerActions, TaskSelectionArea })
        {
            control.Visibility = Visibility.Visible;
            control.IsHitTestVisible = !clean;
            var fade = new DoubleAnimation(control.Opacity, clean ? 0 : 1, duration) { EasingFunction = easing };
            if (clean)
            {
                fade.Completed += (_, _) =>
                {
                    if (cleanFocusMode == true) control.Visibility = Visibility.Hidden;
                };
            }

            control.BeginAnimation(OpacityProperty, fade, HandoffBehavior.SnapshotAndReplace);
        }

        CleanTimerGroup.BeginAnimation(OpacityProperty,
            new DoubleAnimation(CleanTimerGroup.Opacity, clean ? 1 : 0, duration) { EasingFunction = easing },
            HandoffBehavior.SnapshotAndReplace);



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

    private async void SelectedTaskChanged(object? sender, EventArgs e)
    {
        TaskPicker.SelectedValue = taskSelection.SelectedId;
        if (switchingTask) return;
        var previousTask = focusTimer.CurrentTask;
        var pauseMedia = focusTimer.IsRunning && settings.PauseMediaOnPause;
        if (previousTask?.Id != taskSelection.SelectedId && TaskPicker.SelectedItem is FocusTask nextTask)
        {
            var saved = false;
            switchingTask = true;
            ExecuteSafely(() =>
            {
                focusTimer.SwitchTask(nextTask);
                mediaPause.ForgetPausedSessions();
                saved = true;
            });
            if (!saved && previousTask is not null) taskSelection.Select(previousTask.Id);
            switchingTask = false;
            if (!saved) return;
            RefreshDailyRounds();
            Refresh(null, EventArgs.Empty);
            statisticsWindow?.RefreshData();
            if (pauseMedia) await mediaPause.PausePlayingAsync();
        }
        RefreshRoundMarkers();
    }

    private void TasksChanged(object? sender, EventArgs e) => RefreshTaskPicker();

    private void RefreshTaskPicker()
    {
        var selected = focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero
            ? focusTimer.CurrentTask
            : TaskPicker.SelectedItem as FocusTask ?? focusTimer.CurrentTask;
        var available = tasks.ActiveTasks().ToList();

        if (selected is not null && focusTimer.HasStarted && focusTimer.Remaining > TimeSpan.Zero &&
            !available.Any(task => task.Id == selected.Id))
        {
            available.Add(selected);
        }

        RefreshDailyRounds();
        refreshingTaskPicker = true;
        TaskPicker.ItemsSource = available;
        TaskPicker.SelectedItem = available.FirstOrDefault(task => task.Id == selected?.Id)
            ?? available.FirstOrDefault(task => task.Id == Guid.Empty)
            ?? available.First();
        refreshingTaskPicker = false;
        if (TaskPicker.SelectedItem is FocusTask current)
        {
            taskSelection.IsLocked = false;
            taskSelection.Select(current.Id);
        }
        RefreshRoundMarkers();
    }

    private void RefreshDailyRounds()
    {
        var day = DateTime.Today;
        var completed = statistics.ForDay(day).Where(session => session.Completed)
            .GroupBy(session => session.TaskId)
            .ToDictionary(group => group.Key, group => group.Count());
        TaskPicker.Tag = new DailyRounds(settings.FocusMinutes, completed);
        roundsDay = day;
        RefreshRoundMarkers();
    }

    private void RefreshRoundMarkers()
    {
        if (TaskPicker.SelectedItem is not FocusTask task || task.TargetMinutes is not > 0 ||
            TaskPicker.Tag is not DailyRounds rounds)
        {
            CleanRoundMarkers.ItemsSource = null;
            CleanRoundMarkers.Visibility = Visibility.Collapsed;
            return;
        }

        var required = (int)Math.Ceiling(task.TargetMinutes.Value / (double)rounds.FocusMinutes);
        rounds.Completed.TryGetValue(task.Id, out var completed);
        CleanRoundMarkers.ItemsSource = Enumerable.Range(0, required).Select(index => index < completed).ToList();
        CleanRoundMarkers.Visibility = Visibility.Visible;
        System.Windows.Automation.AutomationProperties.SetName(CleanRoundMarkers, $"{completed}/{required} rounds");
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
