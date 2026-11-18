using System.Globalization;
using System.Windows.Data;
using Seal.Models;

namespace Seal.Converters;

public sealed class TaskRoundsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not FocusTask task) return "";
        if (task.TargetMinutes is not > 0 || values[1] is not DailyRounds rounds) return "";
        var required = (int)Math.Ceiling(task.TargetMinutes.Value / (double)rounds.FocusMinutes);
        rounds.Completed.TryGetValue(task.Id, out var completed);
        return $"{completed}/{required} rounds";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
