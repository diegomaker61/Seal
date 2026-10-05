using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Seal.Services;

namespace Seal;

public partial class StatisticsWindow : Window
{
    private readonly StatisticsService statistics;
    private DateTime selectedDay = DateTime.Today;
    private int selectedYear = DateTime.Today.Year;

    public StatisticsWindow(StatisticsService statistics)
    {
        InitializeComponent();
        this.statistics = statistics;
        Loaded += (_, _) => RefreshData();
    }

    private IReadOnlyList<Seal.Models.FocusSession> ReadSessions() =>
        statistics.ReadAll();

    public void RefreshData()
    {
        try
        {
            DrawAnnualActivity();
            DrawDailyActivity();
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            EmptyLabel.Text = "Unable to read history. Check the local data file.";
        }
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
        string[] colors = ["#303030", "#303D40", "#305057", "#2F626B", "#FF2F7580"];

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
                Fill = (Brush)new BrushConverter().ConvertFromString(colors[level])!,
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
        CompletionLabel.Text = sessions.Count == 0 ? "0%" : $"{100.0 * completed / sessions.Count:0}%";
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
                Fill = counts[hour] == 0 ? new SolidColorBrush(Color.FromRgb(56, 56, 56)) : (Brush)FindResource("AccentBrush"),
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
