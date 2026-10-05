using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Seal;

internal static class WindowDrag
{
    public static void Handle(Window window, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject;

        while (source is not null && source != window)
        {
            if (source is ButtonBase or Selector or TextBoxBase)
            {
                return;
            }

            source = source is Visual
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        e.Handled = true;
        window.DragMove();
    }
}
