using System.Runtime.InteropServices;

namespace Seal.Interop;

internal static class ScreenBorderInterop
{
    private const int ExtendedStyleIndex = -20;
    private const int TransparentStyle = 0x20;
    private const int ToolWindowStyle = 0x80;
    private const int NoActivateStyle = 0x08000000;
    private const uint NoActivatePosition = 0x10;
    private const uint FrameChangedPosition = 0x20;
    private delegate bool MonitorCallback(nint monitor, nint context, ref NativeRect rectangle, nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint context, nint clip, MonitorCallback callback, nint data);

    private static readonly nint TopmostWindow = new(-1);

    public static void Configure(nint handle)
    {
        var styles = GetWindowLong(handle, ExtendedStyleIndex);
        SetWindowLong(handle, ExtendedStyleIndex, styles | TransparentStyle | ToolWindowStyle | NoActivateStyle);
    }

    public static IReadOnlyList<MonitorBounds> GetMonitors()
    {
        var monitors = new List<MonitorBounds>();
        MonitorCallback callback = (nint monitor, nint context, ref NativeRect rectangle, nint data) =>
        {
            monitors.Add(new MonitorBounds(rectangle.Left, rectangle.Top,
                rectangle.Right - rectangle.Left, rectangle.Bottom - rectangle.Top));
            return true;
        };

        EnumDisplayMonitors(0, 0, callback, 0);
        return monitors.Distinct().ToList();
    }

    public static void Place(nint handle, MonitorBounds bounds)
    {
        SetWindowPos(handle, TopmostWindow, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            NoActivatePosition | FrameChangedPosition);

        var outer = CreateRectRgn(0, 0, bounds.Width, bounds.Height);
        var inner = CreateRectRgn(2, 2, bounds.Width - 2, bounds.Height - 2);

        try
        {
            CombineRgn(outer, outer, inner, 4);

            if (SetWindowRgn(handle, outer, true) != 0)
            {
                outer = 0;
            }
        }
        finally
        {
            if (outer != 0)
            {
                DeleteObject(outer);
            }

            DeleteObject(inner);
        }
    }

    internal readonly record struct MonitorBounds(int Left, int Top, int Width, int Height);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint window, int index, int value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(nint window, nint region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("gdi32.dll")]
    private static extern nint CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(nint destination, nint first, nint second, int mode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint value);
}
