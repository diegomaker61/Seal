using System.Windows.Input;

namespace Seal.Models;

public sealed record AppSettings(
    int FocusMinutes = 30,
    bool ShowBorder = true,
    bool StartWithWindows = false,
    ModifierKeys ShortcutModifiers = ModifierKeys.Control | ModifierKeys.Alt,
    Key ShortcutKey = Key.P)
{
    public string ShortcutLabel => new KeyGesture(ShortcutKey, ShortcutModifiers).GetDisplayStringForCulture(
        System.Globalization.CultureInfo.InvariantCulture);

    public void Validate()
    {
        if (FocusMinutes is < 10 or > 60)
        {
            throw new ArgumentException("Focus duration must be between 10 and 60 minutes.");
        }

        if ((ShortcutModifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) == 0 ||
            ShortcutKey is Key.None or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            throw new ArgumentException("Choose a key with Ctrl, Alt or Windows.");
        }
    }
}
