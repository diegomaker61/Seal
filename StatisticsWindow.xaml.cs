using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Seal.Services;
using Seal.Models;

namespace Seal;

public partial class StatisticsWindow : Window
{
    private readonly StatisticsService statistics;
    private readonly TaskCatalog tasks;
    private readonly TaskSelection selection;
    private bool refreshingFilters;
    private DateTime selectedDay = DateTime.Today;
    private int selectedYear = DateTime.Today.Year;

    public StatisticsWindow(StatisticsService statistics, TaskCatalog tasks, TaskSelection? selection = null)
    {
        InitializeComponent();
        this.statistics = statistics;
        this.tasks = tasks;
        this.selection = selection ?? new TaskSelection();
        RefreshTaskFilters();
        TaskFilterPicker.SelectionChanged += (_, _) =>
        {
            if (!refreshingFilters && TaskFilterPicker.SelectedItem is TaskFilter filter) this.selection.Select(filter.Id);
            RefreshData();
        };
        this.selection.Changed += SelectionChanged;
        SetSelectionLocked(this.selection.IsLocked);
        tasks.Changed += TasksChanged;
        Closed += (_, _) => { tasks.Changed -= TasksChanged; this.selection.Changed -= SelectionChanged; };
        Loaded += (_, _) => RefreshData();
    }

    public void SetSelectionLocked(bool locked) => TaskFilterPicker.IsEnabled = !locked;

    private void SelectionChanged(object? sender, EventArgs e)
    {
        TaskFilterPicker.SelectedValue = selection.SelectedId;
        RefreshData();
    }

    private IReadOnlyList<FocusSession> ReadSessions()
    {
        if (TaskFilterPicker.SelectedItem is not TaskFilter selected ||
            !tasks.ActiveTasks().Any(task => task.Id == selected.Id))
        {
            return [];
        }

        return statistics.ReadAll().Where(session => session.TaskId == selected.Id).ToList();
    }

    private Color SelectedTaskColor => TaskFilterPicker.SelectedItem is TaskFilter selected
        ? (Color)ColorConverter.ConvertFromString(selected.ColorHex)
        : Color.FromRgb(48, 48, 48);

    private void TasksChanged(object? sender, EventArgs e)
    {
        RefreshTaskFilters();
        RefreshData();
    }

    private void RefreshTaskFilters()
    {
        var selectedId = selection.SelectedId ?? (TaskFilterPicker.SelectedItem as TaskFilter)?.Id;
        var filters = tasks.ActiveTasks()
            .Select(task => new TaskFilter(task.Id, task.Name, task.ColorHex)).ToList();
        refreshingFilters = true;
        TaskFilterPicker.ItemsSource = filters;
        TaskFilterPicker.SelectedItem = filters.FirstOrDefault(filter => filter.Id == selectedId)
            ?? filters.FirstOrDefault();
        refreshingFilters = false;
        if (TaskFilterPicker.SelectedItem is TaskFilter filter) selection.Select(filter.Id);
    }

    public void RefreshData()
    {
        try
        {
            RoundsLabel.Foreground = Brushes.White;
            FocusLabel.Foreground = Brushes.White;
            DrawTargetProgress();
            DrawAnnualActivity();
            DrawDailyActivity();
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            EmptyLabel.Text = "Unable to read history. Check the local data file.";
        }
    }

    private void DrawTargetProgress()
    {
        var selectedId = (TaskFilterPicker.SelectedItem as TaskFilter)?.Id;
        var task = tasks.ActiveTasks().FirstOrDefault(task => task.Id == selectedId);
        TargetLabel.Foreground = Brushes.White;
        if (task?.TargetMinutes is not int target)
        {
            TargetLabel.Text = "—";
            TargetLabel.ToolTip = "No daily goal set";
            return;
        }

        var minutes = ReadSessions().Where(session => session.StartedAt.LocalDateTime.Date == selectedDay.Date)
            .Sum(session => session.FocusSeconds) / 60;
        var reached = minutes >= target;
        TargetLabel.Text = $"{FormatGoalTime(minutes)} / {FormatGoalTime(target)}";
        TargetLabel.ToolTip = $"{(reached ? "Reached" : "In progress")} · {minutes / target * 100:0}% of the daily goal";
    }

    private static string FormatGoalTime(double totalMinutes)
    {
        var minutes = (int)Math.Floor(totalMinutes);
        var hours = minutes / 60;
        var remainder = minutes % 60;
        if (hours == 0) return $"{remainder}m";
        return remainder == 0 ? $"{hours}h" : $"{hours}h {remainder}m";
    }

    private void DrawAnnualActivity()
    {
        AnnualChart.Children.Clear();
        YearLabel.Text = selectedYear.ToString(CultureInfo.InvariantCulture);
        var totals = ReadSessions()
            .GroupBy(session => session.StartedAt.LocalDateTime.Date)
            .ToDictionary(group => group.Key, group => group.Sum(session => session.FocusSeconds));
        var firstDay = new DateTime(selectedYear, 1, 1);
        var start = firstDay.AddDays(-(int)firstDay.DayOfWeek);
        var accent = SelectedTaskColor;
        var colors = Enumerable.Range(0, 5).Select(level => new SolidColorBrush(Color.FromRgb(
            (byte)(48 + (accent.R - 48) * level / 4.0),
            (byte)(48 + (accent.G - 48) * level / 4.0),
            (byte)(48 + (accent.B - 48) * level / 4.0)))).ToArray();

        for (var month = 1; month <= 12; month++)
        {
            var day = new DateTime(selectedYear, month, 1);
            AddLabel(AnnualChart, day.ToString("MMM", CultureInfo.InvariantCulture), (int)((day - start).TotalDays / 7) * 14, 0);
        }

        for (var day = firstDay; day.Year == selectedYear; day = day.AddDays(1))
        {
            totals.TryGetValue(day, out var seconds);
            var level = seconds <= 0 ? 0 : Math.Min(4, (int)Math.Ceiling(seconds / 1800));
            var cell = new Rectangle
            {
                Width = 11,
                Height = 11,
                RadiusX = 2,
                RadiusY = 2,
                Fill = colors[level],
                Stroke = day == selectedDay ? Brushes.White : Brushes.Transparent,
                StrokeThickness = 1,
                ToolTip = $"{day:yyyy-MM-dd}: {seconds / 60:0} focus minutes",
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var date = day;
            cell.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                selectedDay = date;
                RefreshData();
            };
            Canvas.SetLeft(cell, (int)((day - start).TotalDays / 7) * 14);
            Canvas.SetTop(cell, 24 + (int)day.DayOfWeek * 14);
            AnnualChart.Children.Add(cell);
        }
    }

    private void DrawDailyActivity()
    {
        var sessions = ReadSessions().Where(session => session.StartedAt.LocalDateTime.Date == selectedDay.Date).ToList();
        var completed = sessions.Count(session => session.Completed);
        var minutes = sessions.Sum(session => session.FocusSeconds) / 60;
        DayLabel.Text = selectedDay.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
        RoundsLabel.Text = completed.ToString(CultureInfo.InvariantCulture);
        FocusLabel.Text = minutes >= 60 ? $"{(int)(minutes / 60)}h {minutes % 60:0}m" : $"{minutes:0}m";
        EmptyLabel.Text = sessions.Count == 0 ? "No sessions recorded for this day." : "";
        HourlyChart.Children.Clear();
        var counts = Enumerable.Range(0, 24)
            .Select(hour => sessions.Count(session => session.Completed && session.StartedAt.LocalDateTime.Hour == hour))
            .ToArray();
        var maximum = Math.Max(1, counts.Max());

        for (var hour = 0; hour < 24; hour++)
        {
            var height = counts[hour] == 0 ? 2 : 90.0 * counts[hour] / maximum;
            var bar = new Rectangle
            {
                Width = 22,
                Height = height,
                RadiusX = 2,
                RadiusY = 2,
                Fill = counts[hour] == 0 ? new SolidColorBrush(Color.FromRgb(56, 56, 56)) : new SolidColorBrush(SelectedTaskColor),
                ToolTip = $"{hour:00}:00 — {counts[hour]} completed sessions"
            };
            Canvas.SetLeft(bar, hour * 31);
            Canvas.SetTop(bar, 95 - height);
            HourlyChart.Children.Add(bar);

            if (hour % 6 == 0 || hour == 23)
            {
                AddLabel(HourlyChart, $"{hour:00}:00", hour * 31, 105);
            }
        }
    }

    private static void AddLabel(Canvas canvas, string text, double left, double top)
    {
        var label = new TextBlock { Text = text, FontSize = 10, Foreground = Brushes.DarkGray };
        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        canvas.Children.Add(label);
    }

    private void PreviousYear(object sender, RoutedEventArgs e)
    {
        selectedYear = Math.Max(2, selectedYear - 1);
        RefreshData();
    }

    private void NextYear(object sender, RoutedEventArgs e)
    {
        selectedYear = Math.Min(9998, selectedYear + 1);
        RefreshData();
    }

    private void PreviousDay(object sender, RoutedEventArgs e) => SelectDay(selectedDay.AddDays(-1));
    private void NextDay(object sender, RoutedEventArgs e) => SelectDay(selectedDay.AddDays(1));
    private void SelectToday(object sender, RoutedEventArgs e) => SelectDay(DateTime.Today);
    private void Reload(object sender, RoutedEventArgs e) => RefreshData();

    private void DragWindow(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        WindowDrag.Handle(this, e);
    }

    private void CloseWindow(object sender, RoutedEventArgs e) => Close();

    private void SelectDay(DateTime day)
    {
        selectedDay = day;
        selectedYear = day.Year;
        RefreshData();
    }
}
