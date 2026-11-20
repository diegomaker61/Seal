using System.Globalization;
using System.Windows.Data;

namespace Seal.Converters;

public sealed class TaskDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrEmpty(name)) return "";
        var first = StringInfo.GetNextTextElement(name);
        return first.ToUpper(culture) + name[first.Length..];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
