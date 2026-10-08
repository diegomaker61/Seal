using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using Seal.Models;

namespace Seal.Services;

public sealed class GlobalTimerShortcut : IDisposable
{
    private const int ShortcutId = 1;
    private const int HotkeyMessage = 0x312;
    private AppSettings settings;
    private int currentId = ShortcutId;
    private const uint Key = 0x50;
    private readonly Window window;
    private readonly Action toggleTimer;
    private HwndSource? source;
    private nint handle;
    private bool disposed;

    public GlobalTimerShortcut(Window window, Action toggleTimer, AppSettings? settings = null)
    {
        this.settings = settings ?? new AppSettings();
        this.window = window;
        this.toggleTimer = toggleTimer;
        window.SourceInitialized += Register;
    }

    public bool IsRegistered { get; private set; }

    private void Register(object? sender, EventArgs e)
    {
        handle = new WindowInteropHelper(window).Handle;
        source = HwndSource.FromHwnd(handle);
        source?.AddHook(HandleMessage);
        IsRegistered = RegisterHotKey(handle, currentId, (uint)settings.ShortcutModifiers | 0x4000, (uint)KeyInterop.VirtualKeyFromKey(settings.ShortcutKey));
    }

    public bool TryChange(AppSettings updated)
    {
        updated.Validate();

        if (IsRegistered && settings.ShortcutKey == updated.ShortcutKey && settings.ShortcutModifiers == updated.ShortcutModifiers)
        {
            settings = updated;
            return true;
        }

        var nextId = currentId == 1 ? 2 : 1;
        if (!RegisterHotKey(handle, nextId, (uint)updated.ShortcutModifiers | 0x4000,
            (uint)KeyInterop.VirtualKeyFromKey(updated.ShortcutKey)))
        {
            return false;
        }

        if (IsRegistered)
        {
            UnregisterHotKey(handle, currentId);
        }

        currentId = nextId;
        settings = updated;
        IsRegistered = true;
        return true;
    }

    private nint HandleMessage(nint windowHandle, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (IsRegistered && message == HotkeyMessage && wParam == currentId)
        {
            handled = true;
            toggleTimer();
        }

        return 0;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        window.SourceInitialized -= Register;
        source?.RemoveHook(HandleMessage);

        if (IsRegistered)
        {
            UnregisterHotKey(handle, currentId);
            IsRegistered = false;
        }

        disposed = true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);
}
