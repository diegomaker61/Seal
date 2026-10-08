using Seal.Interop;

namespace Seal.Services;

public sealed class FocusBorderController : IDisposable
{
    private readonly Dictionary<ScreenBorderInterop.MonitorBounds, FocusBorderWindow> borders = [];

    public void UpdateVisibility(bool isRunning)
    {
        if (!isRunning)
        {
            foreach (var border in borders.Values)
            {
                border.Hide();
            }

            return;
        }

        var monitors = ScreenBorderInterop.GetMonitors();

        foreach (var removed in borders.Keys.Except(monitors).ToList())
        {
            borders[removed].Close();
            borders.Remove(removed);
        }

        foreach (var monitor in monitors)
        {
            if (!borders.TryGetValue(monitor, out var border))
            {
                border = new FocusBorderWindow();
                borders.Add(monitor, border);
            }

            border.ShowOnMonitor(monitor);
        }
    }

    public void Dispose()
    {
        foreach (var border in borders.Values)
        {
            border.Close();
        }

        borders.Clear();
    }
}
