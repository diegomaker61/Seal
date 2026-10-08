using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Seal.Interop;

namespace Seal;

public sealed class FocusBorderWindow : Window
{
    private nint handle;
    private ScreenBorderInterop.MonitorBounds? currentBounds;

    public FocusBorderWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromRgb(3, 252, 236));
        Opacity = 0.65;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        Topmost = true;
        Width = 1;
        Height = 1;
        SourceInitialized += InitializeOverlay;
    }

    internal void ShowOnMonitor(ScreenBorderInterop.MonitorBounds bounds)
    {
        if (!IsVisible)
        {
            new WindowInteropHelper(this).EnsureHandle();
            ScreenBorderInterop.Place(handle, bounds);
            Show();
            currentBounds = null;
        }

        if (currentBounds != bounds)
        {
            ScreenBorderInterop.Place(handle, bounds);
            currentBounds = bounds;
        }
    }

    private void InitializeOverlay(object? sender, EventArgs e)
    {
        handle = new WindowInteropHelper(this).Handle;
        ScreenBorderInterop.Configure(handle);
        HwndSource.FromHwnd(handle)?.AddHook(HandleMessage);
    }

    private nint HandleMessage(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == 0x84)
        {
            handled = true;
            return new nint(-1);
        }

        if (message == 0x21)
        {
            handled = true;
            return new nint(3);
        }

        return 0;
    }
}
